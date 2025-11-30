using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using TaskOrganizer.Models;
using TaskOrganizer.Services;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http; // ❗ NEW: Para sa IFormFile
using Microsoft.AspNetCore.Hosting; // ❗ NEW: Para sa IWebHostEnvironment
using System.IO; // ❗ NEW: Para sa Path at FileStream

namespace TaskOrganizer.Pages
{
    public class AssignTaskModel : PageModel
    {
        private readonly TaskService _taskService;
        private readonly EmployeeServices _employeeService;
        private readonly IWebHostEnvironment _webHostEnvironment; // ❗ NEW: Para sa pag-save ng file

        public AssignTaskModel(TaskService taskService, EmployeeServices employeeService, IWebHostEnvironment webHostEnvironment)
        {
            _taskService = taskService;
            _employeeService = employeeService;
            _webHostEnvironment = webHostEnvironment; // ❗ NEW: Inject the environment
        }

        [BindProperty]
        public TaskInputModel Input { get; set; } = new TaskInputModel();

        public List<SelectListItem> EmployeeList { get; set; } = new List<SelectListItem>();

        public class TaskInputModel
        {
            [Required(ErrorMessage = "Task Title is required.")]
            public string Title { get; set; } = string.Empty;

            [Required(ErrorMessage = "Description is required.")]
            public string Description { get; set; } = string.Empty;

            [Required(ErrorMessage = "Employee is required.")]
            [BindProperty(Name = "employee")]
            public string EmployeeId { get; set; } = string.Empty;

            [Required(ErrorMessage = "Priority is required.")]
            [BindProperty(Name = "priority")]
            public string Priority { get; set; } = "Medium";

            [BindProperty(Name = "due_date")]
            [DataType(DataType.Date)]
            public DateTime? DueDate { get; set; }

            // BAGONG PROPERTY PARA SA ORAS
            [BindProperty(Name = "due_time")]
            [DataType(DataType.Time)]
            public string? DueTime { get; set; }

            // ❗ NEW: Property para saluhin ang in-upload na file ❗
            [BindProperty(Name = "AttachmentFile")]
            public IFormFile? AttachmentFile { get; set; }
        }

        public async System.Threading.Tasks.Task OnGetAsync()
        {
            await PopulateEmployeeListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await PopulateEmployeeListAsync();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            string attachmentPath = null; // Variable para i-save ang path/URL ng file

            // ❗ NEW: File Handling Logic ❗
            if (Input.AttachmentFile != null)
            {
                // 1. I-set up ang uploads folder path (e.g., wwwroot/attachments)
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "attachments");

                // 2. Gumawa ng folder kung wala pa
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // 3. Gumawa ng unique filename
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Input.AttachmentFile.FileName;

                // 4. Ito ang path na ise-save sa database (relative path)
                attachmentPath = "/attachments/" + uniqueFileName;

                // 5. Kumpletong path sa server
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // 6. I-save ang file sa server
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await Input.AttachmentFile.CopyToAsync(fileStream);
                }
            }
            // ❗ END File Handling Logic ❗


            DateTime? fullDueDate = Input.DueDate;

            // Logic: Pagsamahin ang DueDate at DueTime
            if (fullDueDate.HasValue && !string.IsNullOrWhiteSpace(Input.DueTime))
            {
                // I-parse ang oras (e.g., "14:30")
                if (TimeSpan.TryParse(Input.DueTime, out TimeSpan timeSpan))
                {
                    // I-combine ang petsa (DateOnly) at oras (TimeSpan)
                    fullDueDate = fullDueDate.Value.Date.Add(timeSpan);
                }
            }
            // Kung walang DueDate o DueTime, mananatili itong null.

            var newTask = new TaskOrganizer.Models.Task
            {
                Title = Input.Title,
                Description = Input.Description,
                EmployeeId = Input.EmployeeId,
                Priority = Input.Priority,
                DueDate = fullDueDate, // Ginagamit na ang pinagsamang DateTime
                // ❗ NEW: Idagdag ang AttachmentPath sa inyong Task Model ❗
                // Tandaan: Dapat may property na `AttachmentUrl` or katulad nito sa inyong TaskOrganizer.Models.Task
                AttachmentUrl = attachmentPath
            };

            await _taskService.AssignNewTaskAsync(newTask);

            TempData["SuccessMessage"] = $"Task '{newTask.Title}' successfully assigned.";

            return RedirectToPage("/AllTasks");
        }

        private async System.Threading.Tasks.Task PopulateEmployeeListAsync()
        {
            var employees = await _employeeService.GetAllEmployeesAsync();

            EmployeeList = employees.Select(e => new SelectListItem
            {
                Value = e.Id,
                Text = e.Name
            }).ToList();
        }
    }
}