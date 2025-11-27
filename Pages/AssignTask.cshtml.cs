using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using TaskOrganizer.Models;
using TaskOrganizer.Services;
using System.Threading.Tasks;

namespace TaskOrganizer.Pages
{
    public class AssignTaskModel : PageModel
    {
        private readonly TaskService _taskService;
        private readonly EmployeeServices _employeeService;

        // I-re-declare ang using directive para ma-resolve ang conflict sa Task Model 
        // using TaskModel = TaskOrganizer.Models.Task;

        public AssignTaskModel(TaskService taskService, EmployeeServices employeeService)
        {
            _taskService = taskService;
            _employeeService = employeeService;
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
                DueDate = fullDueDate // Ginagamit na ang pinagsamang DateTime
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