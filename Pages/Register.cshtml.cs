// TaskOrganizer.Pages.Register/Register.cshtml.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Net; // Para sa NetworkCredential
using System.Net.Mail; // Para sa SmtpException
using System.Security.Cryptography;
using System.Text;
using TaskOrganizer.Models; // Tiyakin na nandito ang Admin at Employee Models
using TaskOrganizer.Services; // Tiyakin na nandito ang EmployeeService, AdminService, at EmailService

namespace TaskOrganizer.Pages.Register
{
    public class RegisterModel : PageModel
    {
        private readonly EmployeeServices _employeeService;
        private readonly AdminService _adminService;
        private readonly EmailService _emailService;

        public RegisterModel(EmployeeServices employeeService, AdminService adminService, EmailService emailService)
        {
            _employeeService = employeeService;
            _adminService = adminService;
            _emailService = emailService;
        }

        [BindProperty]
        public RegisterInputModel Input { get; set; } = new RegisterInputModel();

        public void OnGet() { }

        public async Task<IActionResult> OnPost()
        {
            // Tiyakin na ang lahat ng fields ay valid (hal. password match, required fields)
            if (!ModelState.IsValid)
                return Page();

            var passwordHash = HashPassword(Input.Password);
            bool registrationSuccess = false;

            if (Input.Role == "Admin")
            {
                // ADMINISTRATOR REGISTRATION LOGIC

                // 1. Check for existing Email (Admin)
                var existingAdminEmail = await _adminService.GetByEmailAsync(Input.Email);
                if (existingAdminEmail != null)
                {
                    ModelState.AddModelError("Input.Email", "Email already registered as an Admin.");
                    return Page();
                }

                // 2. Check for existing Email (Employee) - Cross-check
                var existingEmployeeEmail = await _employeeService.GetByEmailAsync(Input.Email);
                if (existingEmployeeEmail != null)
                {
                    ModelState.AddModelError("Input.Email", "Email is already linked to an Employee account.");
                    return Page();
                }

                var admin = new Admin
                {
                    Name = Input.Name,
                    Username = Input.Username,
                    Email = Input.Email,
                    PasswordHash = passwordHash
                };
                await _adminService.CreateAsync(admin); // I-save sa Admins collection
                registrationSuccess = true;
            }
            else // Employee Registration Logic
            {
                // EMPLOYEE REGISTRATION LOGIC

                // 1. Check for existing Email (Employee)
                var existingEmployeeEmail = await _employeeService.GetByEmailAsync(Input.Email);
                if (existingEmployeeEmail != null)
                {
                    ModelState.AddModelError("Input.Email", "Email already registered as an Employee.");
                    return Page();
                }

                // 2. Check for existing Email (Admin) - Cross-check
                var existingAdminEmail = await _adminService.GetByEmailAsync(Input.Email);
                if (existingAdminEmail != null)
                {
                    ModelState.AddModelError("Input.Email", "Email is already linked to an Admin account.");
                    return Page();
                }

                var employee = new Employee
                {
                    Name = Input.Name,
                    Username = Input.Username,
                    Email = Input.Email,
                    PasswordHash = passwordHash
                };
                await _employeeService.CreateAsync(employee); // I-save sa Employees collection
                registrationSuccess = true;
            }

            // Kung successful ang pag-save sa database:
            if (registrationSuccess)
            {
                // I-ISOLATE ANG EMAIL SENDING GAMIT ANG TRY-CATCH
                try
                {
                    // Send email notification (common)
                    _emailService.SendRegistrationEmail(Input.Email, Input.Name);
                }
                catch (SmtpException ex)
                {
                    // Kung mag-fail ang email, tuloy pa rin ang user sa success page.
                    Console.WriteLine($"WARNING: Failed to send registration email to {Input.Email}. Error: {ex.Message}");
                    TempData["EmailError"] = "Registration successful, but we could not send the confirmation email.";
                }
                catch (System.Net.Sockets.SocketException ex)
                {
                    // Kung mag-fail ang koneksyon, tuloy pa rin.
                    Console.WriteLine($"WARNING: Socket connection failed for email service. Error: {ex.Message}");
                    TempData["EmailError"] = "Registration successful, but there was a connection error sending the confirmation email.";
                }
            }

            return RedirectToPage("/Success");
        }

        private string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }
    }

    // ✨ RegisterInputModel Definition (Ito ang nagko-cause ng error dati)
    // Tiyakin na nasa loob ito ng TaskOrganizer.Pages.Register namespace.
    public class RegisterInputModel
    {
        [Required(ErrorMessage = "Full Name is required.")]
        public string Name { get; set; } = "";

        [Required(ErrorMessage = "Username is required.")]
        public string Username { get; set; } = "";

        [Required(ErrorMessage = "Please enter your email address.")]
        [EmailAddress(ErrorMessage = "Oops! That doesn't look like a valid email.")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Please enter your password.")]
        [DataType(DataType.Password)]
        [StringLength(100, ErrorMessage = "The password must be at least {2} characters long.", MinimumLength = 6)]
        public string Password { get; set; } = "";

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = "";

        [Required(ErrorMessage = "Role is required.")]
        public string Role { get; set; } = "Employee";
    }
}