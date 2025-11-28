using Microsoft.AspNetCore.Mvc.RazorPages;
using TaskOrganizer.Services;
using System.Linq;
using System.Security.Claims; // Importante para sa Claims

namespace TaskOrganizer.Pages
{
    public class ReportsModel : PageModel
    {
        private readonly TaskService _taskService;

        // Properties para sa Summary Counts
        public int TotalTasksCount { get; set; }
        public int CompletedTasksCount { get; set; }
        public int PendingTasksCount { get; set; }
        public int HighPriorityTasksCount { get; set; }
        public double CompletionPercentage { get; set; } = 0;
        public string EmployeeUsername { get; set; } = "User";


        // I-inject ang TaskService sa Constructor
        public ReportsModel(TaskService taskService)
        {
            _taskService = taskService;
        }

        public async System.Threading.Tasks.Task OnGetAsync()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                // KUNIN ANG EMPLOYEE ID (Galing sa custom "EmployeeId" claim)
                EmployeeUsername = User.Identity.Name ?? "Employee";

                // 2. KUNIN ANG EMPLOYEE ID (Galing sa custom "EmployeeId" claim)
                // CRITICAL: Ito ang gagamitin para sa filtering.
                string? employeeId = User.Claims
                    .FirstOrDefault(c => c.Type == "EmployeeId")?.Value;

                if (string.IsNullOrEmpty(employeeId))
                {
                    // Kung walang ID claim (e.g., luma ang cookie), mag-exit.
                    return;
                }

                // 3. Kunin ang tasks gamit ang tamang ID (FINALLY! Filtered na ito!)
                var allTasks = await _taskService.GetTasksByEmployeeIdAsync(employeeId);
                // Count the tasks (Parehong logic sa DashboardModel)
                TotalTasksCount = allTasks.Count;
                CompletedTasksCount = allTasks.Count(t => t.Status == "Completed");
                PendingTasksCount = allTasks.Count(t => t.Status == "Pending" || t.Status == "In Progress");
                HighPriorityTasksCount = allTasks.Count(t => t.Priority == "High" && t.Status != "Completed");

                // Calculate Completion Percentage (Parehong logic sa DashboardModel)
                if (TotalTasksCount > 0)
                {
                    CompletionPercentage = (double)CompletedTasksCount / TotalTasksCount * 100;
                }

                // NOTE: Pwede ka ring magdagdag ng iba pang report-specific data dito (e.g., tasks per month)
            }
        }
    }
}