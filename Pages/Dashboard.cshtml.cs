using Microsoft.AspNetCore.Mvc.RazorPages;
using TaskOrganizer.Models;
using TaskOrganizer.Services;
using System.Linq;
using System.Security.Claims; // ❗ IMPORTANTE: Para makuha ang Claims

namespace TaskOrganizer.Pages
{
    public class DashboardModel : PageModel
    {
        private readonly TaskService _taskService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        // Properties para sa Summary Cards
        public int TotalTasksCount { get; set; }
        public int CompletedTasksCount { get; set; }
        public int PendingTasksCount { get; set; }
        public int HighPriorityTasksCount { get; set; }
        // Ngayon, ang EmployeeUsername ay kukunin ang 'Name' Claim, hindi Email.
        public string EmployeeUsername { get; set; } = "User";

        public List<TaskOrganizer.Models.Task> RecentCompletedTasks { get; set; } = new List<TaskOrganizer.Models.Task>();
        public double CompletionPercentage { get; set; } = 0;

        public DashboardModel(TaskService taskService, IHttpContextAccessor httpContextAccessor)
        {
            _taskService = taskService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async System.Threading.Tasks.Task OnGetAsync()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                // 1. KUNIN ANG DISPLAY NAME (Galing sa ClaimTypes.Name)
                // Ito ay ang Employee.Name na ni-set natin sa Login.cshtml.cs
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

                // 4. Count the tasks
                TotalTasksCount = allTasks.Count;
                CompletedTasksCount = allTasks.Count(t => t.Status == "Completed");
                PendingTasksCount = allTasks.Count(t => t.Status == "Pending" || t.Status == "In Progress");
                HighPriorityTasksCount = allTasks.Count(t => t.Priority == "High" && t.Status != "Completed");

                // 5. Calculate Completion Percentage
                if (TotalTasksCount > 0)
                {
                    CompletionPercentage = (double)CompletedTasksCount / TotalTasksCount * 100;
                }

                // 6. Kunin ang Recent Completed Tasks
                RecentCompletedTasks = allTasks
                    .Where(t => t.Status == "Completed" && t.CompletionDate.HasValue)
                    .OrderByDescending(t => t.CompletionDate)
                    .Take(5)
                    .ToList();
            }
        }
    }
}