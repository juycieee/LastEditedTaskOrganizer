// TaskOrganizer.Pages.Login/Login.cshtml.cs

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using TaskOrganizer.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace TaskOrganizer.Pages.Login
{
    public class LoginModel : PageModel
    {
        private readonly EmployeeServices _employeeService;
        private readonly AdminService _adminService;

        public LoginModel(EmployeeServices employeeService, AdminService adminService)
        {
            _employeeService = employeeService;
            _adminService = adminService;
        }

        [BindProperty]
        public LoginInputModel Input { get; set; } = new LoginInputModel();

        public void OnGet() { }

        public async Task<IActionResult> OnPost()
        {
            if (!ModelState.IsValid)
                return Page();

            var hashed = HashPassword(Input.Password);

            // Note: Ang 'valid' variable ay hindi na kailangan para sa Employee.

            if (Input.Role == "Admin")
            {
                bool validAdmin = await _adminService.ValidateLogin(Input.Email, hashed);

                if (validAdmin)
                {
                    // Assuming you have an Authenticate method for Admin
                    // You might need to adjust this part based on your Admin setup
                    return RedirectToPage("/AdminDashboard");
                }
            }
            else // Employee Login Check
            {
                // ❗ CRITICAL FIX #1: GUMAMIT NG GetValidEmployee (na nagbabalik ng Employee object) ❗
                var employee = await _employeeService.GetValidEmployee(Input.Email, hashed);

                if (employee != null)
                {
                    // ❗ CRITICAL FIX #2: PUMASA NG EMPLOYEE DATA sa Authenticate method ❗
                    // employee.Id (para sa filtering) at employee.Name (para sa display)
                    await Authenticate(employee.Id!, employee.Name, employee.Email, Input.Role);
                    return RedirectToPage("/Dashboard");
                }
            }

            // Kung hindi nag-match ang credentials o role
            ModelState.AddModelError("", "Invalid login attempt. Please check your credentials and selected account type.");
            return Page();
        }

        private async System.Threading.Tasks.Task Authenticate(string employeeId, string displayName, string email, string role)
        {
            var claims = new List<Claim>
            {
                // CRITICAL: Id para sa filtering (gagamitin sa TaskService)
                new Claim("EmployeeId", employeeId), 
                
                // CRITICAL: Name para sa display sa Dashboard (Ito ang magiging User.Identity.Name)
                new Claim(ClaimTypes.Name, displayName), 
                
                // Email (Optional, pero maganda kung meron)
                new Claim(ClaimTypes.Email, email), 
                
                // Role
                new Claim(ClaimTypes.Role, role)
            };

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity));
        }


        // Hashing function
        private string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }
    }

    // ✨ FIXED: Kumpletong LoginInputModel
    public class LoginInputModel
    {
        [Required(ErrorMessage = "Please enter your email address.")]
        [EmailAddress(ErrorMessage = "Oops! That doesn't look like a valid email.")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Please enter your password.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Please select your account type.")]
        public string Role { get; set; } = "Employee";
    }
}