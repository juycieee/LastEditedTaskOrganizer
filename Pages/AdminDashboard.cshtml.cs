using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Linq; // Para sa .Count() at .Count(t => t.Status == "...")
using TaskOrganizer.Services; // Para sa TaskService at EmployeeServices
using TaskOrganizer.Models; // Kailangan para ma-reference ang Task at Employee models
using System.Collections.Generic;

// Tandaan: Inalis na natin ang 'using System.Threading.Tasks;'
// at sa halip, gagamitin natin ang 'System.Threading.Tasks.Task' sa method signature.

namespace TaskOrganizer.Pages
{
    public class AdminDashboardModel : PageModel
    {
        private readonly TaskService _taskService;
        private readonly EmployeeServices _employeeService;

        // Dashboard Data Properties
        public int TotalEmployees { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int PendingTasks { get; set; }

        public AdminDashboardModel(TaskService taskService, EmployeeServices employeeService)
        {
            _taskService = taskService;
            _employeeService = employeeService;
        }

        // ❗ FIX: Ginawang fully qualified ang return type ng Task ❗
        public async System.Threading.Tasks.Task OnGetAsync()
        {
            // 1. Employee Count
            var allEmployees = await _employeeService.GetAllEmployeesAsync();
            TotalEmployees = allEmployees.Count();

            // 2. Task Counts
            var allTasks = await _taskService.GetAllTasksAsync();

            TotalTasks = allTasks.Count();

            CompletedTasks = allTasks.Count(t => t.Status == "Completed");

            // Bilangin ang Pending tasks (In Progress, Pending)
            PendingTasks = allTasks.Count(t => t.Status == "Pending" || t.Status == "In Progress");
        }
    }
}