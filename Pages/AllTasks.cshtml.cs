using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TaskOrganizer.Models;
using TaskOrganizer.Services;
using System.Collections.Generic;
using System.Linq; // Kailangan para sa Where() method
using System.Threading.Tasks;

namespace TaskOrganizer.Pages
{
    public class AllTasksModel : PageModel
    {
        private readonly TaskService _taskService;
        private readonly EmployeeServices _employeeService;

        // --- PROPERTIES PARA SA FILTERING ---

        // BindProperty(SupportsGet = true) - Para makuha ang value mula sa URL (?PriorityFilter=High)
        [BindProperty(SupportsGet = true)]
        public string PriorityFilter { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string EmployeeFilter { get; set; } = string.Empty; // Holds EmployeeId

        [BindProperty(SupportsGet = true)]
        public string StatusFilter { get; set; } = string.Empty;

        // --- PROPERTIES PARA SA DATA DISPLAY ---

        public IList<TaskOrganizer.Models.Task> Tasks { get; set; } = new List<TaskOrganizer.Models.Task>();
        public IList<Employee> EmployeeList { get; set; } = new List<Employee>();

        public AllTasksModel(TaskService taskService, EmployeeServices employeeService)
        {
            _taskService = taskService;
            _employeeService = employeeService;
        }

        public async System.Threading.Tasks.Task OnGetAsync()
        {
            // 1. Kukunin ang LAHAT ng Tasks
            var allTasks = await _taskService.GetAllTasksAsync();

            // 2. Kukunin ang LAHAT ng Employees
            EmployeeList = await _employeeService.GetAllEmployeesAsync();

            // 3. I-APPLY ang Filtering Logic
            IEnumerable<TaskOrganizer.Models.Task> filteredTasks = allTasks;

            // ❗ KRITIKAL: I-filter para TANGING mga task na HINDI Archived ang makita ❗
            filteredTasks = filteredTasks.Where(t => t.Status != "Archived");

            // Filter by Priority
            if (!string.IsNullOrEmpty(PriorityFilter))
            {
                filteredTasks = filteredTasks.Where(t => t.Priority == PriorityFilter);
            }

            // Filter by Employee
            if (!string.IsNullOrEmpty(EmployeeFilter))
            {
                filteredTasks = filteredTasks.Where(t => t.EmployeeId == EmployeeFilter);
            }

            // Filter by Status
            if (!string.IsNullOrEmpty(StatusFilter))
            {
                filteredTasks = filteredTasks.Where(t => t.Status == StatusFilter);
            }

            // Final result ay i-co-convert pabalik sa List
            Tasks = filteredTasks.ToList();
        }

        // ❗ DITO ANG ONPOST HANDLER PARA SA ARCHIVE/DELETE BUTTON ❗
        public async Task<IActionResult> OnPostDeleteAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToPage();
            }

            // Gagamitin ang "Archived" status para hindi ma-delete sa database.
            // Awtomatikong mawawala ito sa OnGetAsync listahan dahil sa filter sa taas.
            await _taskService.UpdateTaskStatusAsync(id, "Archived");

            // I-redirect pabalik sa AllTasks page
            return RedirectToPage();
        }
    }
}