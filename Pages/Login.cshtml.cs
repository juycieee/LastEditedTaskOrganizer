using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using TaskOrganizer.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using TaskOrganizer.Models; // ✅ IDINAGDAG: Para ma-access ang Admin/Employee models

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

            if (Input.Role == "Admin")
            {
                // ✅ PAGBABAGO: Kumuha ng Admin object pagkatapos ng validation
                // Assuming may GetValidAdmin method sa AdminService na nagbabalik ng Admin object (tulad ng Employee)
                var admin = await _adminService.GetValidAdmin(Input.Email, hashed);

                if (admin != null)
                {
                    // ✅ PAGBABAGO: Gumamit ng Authenticate method
                    // Assumed na ang Admin model ay mayroong Id, Name, at Email properties.
                    await Authenticate(admin.Id!, admin.Name, admin.Email, Input.Role);
                    return RedirectToPage("/AdminDashboard");
                }
            }
            else // Employee Login Check
            {
                var employee = await _employeeService.GetValidEmployee(Input.Email, hashed);

                if (employee != null)
                {
                    // Gumagamit na ng Authenticate
                    await Authenticate(employee.Id!, employee.Name, employee.Email, Input.Role);
                    return RedirectToPage("/Dashboard");
                }
            }

            // Kung hindi nag-match ang credentials o role
            ModelState.AddModelError("", "Invalid login attempt. Please check your credentials and selected account type.");
            return Page();
        }

        // WALANG PAGBABAGO DITO - NAKA-SET SA "EmployeeId" CLAIM
        private async System.Threading.Tasks.Task Authenticate(string userId, string displayName, string email, string role)
        {
            var claims = new List<Claim>
            {
                // Ginamit ang "EmployeeId" claim para sa ID, na consistent sa MyTasks.cshtml.cs
                new Claim("EmployeeId", userId),
                new Claim(ClaimTypes.Name, displayName),
                new Claim(ClaimTypes.Email, email),
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