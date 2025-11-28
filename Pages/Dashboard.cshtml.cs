using Microsoft.AspNetCore.Mvc.RazorPages;
using TaskOrganizer.Models;
using TaskOrganizer.Services;
using System.Linq;
using System.Security.Claims;
using System.Collections.Generic; // Added for List
using System; // Added for DateTime and TimeZoneInfo
using TaskOrganizer.Models; // Ensure AppTask is correctly referenced
using AppTask = TaskOrganizer.Models.Task; // Use alias to avoid conflict

namespace TaskOrganizer.Pages
{
    public class DashboardModel : PageModel
    {
        private readonly TaskService _taskService;

        // Removed: IHttpContextAccessor is not strictly needed here if we use User property
        // private readonly IHttpContextAccessor _httpContextAccessor; 

        // Properties para sa Summary Cards
        public int TotalTasksCount { get; set; }
        public int CompletedTasksCount { get; set; }
        public int PendingTasksCount { get; set; }
        public int HighPriorityTasksCount { get; set; }

        // ❗ BAGONG PROPERTY: Missing/Overdue Tasks Count ❗
        public int OverdueTasksCount { get; set; }

        public string EmployeeUsername { get; set; } = "User";

        public List<AppTask> RecentCompletedTasks { get; set; } = new List<AppTask>();
        public double CompletionPercentage { get; set; } = 0;

        public DashboardModel(TaskService taskService)
        {
            _taskService = taskService;
            // No need to inject IHttpContextAccessor if not used
        }

        // Helper Method para sa Time Zone Conversion (Ginagamit ang PH Time Zone)
        private TimeZoneInfo GetPhilippineTimeZone()
        {
            try
            {
                // Standard ID for Windows and Linux/macOS
                return TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");
                }
                catch
                {
                    return TimeZoneInfo.Utc;
                }
            }
        }

        public async System.Threading.Tasks.Task OnGetAsync()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                // 1. KUNIN ANG DISPLAY NAME
                EmployeeUsername = User.Identity.Name ?? "Employee";

                // 2. KUNIN ANG EMPLOYEE ID
                string? employeeId = User.Claims
                    .FirstOrDefault(c => c.Type == "EmployeeId")?.Value;

                if (string.IsNullOrEmpty(employeeId))
                {
                    return;
                }

                // 3. Kunin ang tasks
                var allTasks = await _taskService.GetTasksByEmployeeIdAsync(employeeId);
                var pendingTasks = allTasks.Where(t => t.Status != "Completed" && t.Status != "Archived").ToList();

                // 4. Time Zone at Overdue Check Setup
                TimeZoneInfo phTimeZone = GetPhilippineTimeZone();
                DateTime phNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, phTimeZone);
                int overdueCounter = 0;

                // 5. Loop para i-check ang Overdue tasks
                foreach (var task in pendingTasks)
                {
                    if (task.DueDate.HasValue)
                    {
                        DateTime utcTime = task.DueDate.Value;

                        // Siguraduhin na UTC ang Kind para tama ang conversion
                        if (task.DueDate.Value.Kind != DateTimeKind.Utc)
                        {
                            utcTime = DateTime.SpecifyKind(task.DueDate.Value, DateTimeKind.Utc);
                        }

                        // I-convert ang DueDate sa Philippine Time
                        DateTime phDueDate = TimeZoneInfo.ConvertTimeFromUtc(utcTime, phTimeZone);

                        // I-check kung overdue na ito
                        if (phDueDate < phNow)
                        {
                            overdueCounter++;
                        }
                    }
                }

                // 6. I-set ang Counts
                TotalTasksCount = allTasks.Count;
                CompletedTasksCount = allTasks.Count(t => t.Status == "Completed");
                PendingTasksCount = allTasks.Count(t => t.Status == "Pending" || t.Status == "In Progress");
                HighPriorityTasksCount = allTasks.Count(t => t.Priority == "High" && t.Status != "Completed");

                // ❗ I-set ang Overdue Count ❗
                OverdueTasksCount = overdueCounter;

                // 7. Calculate Completion Percentage
                if (TotalTasksCount > 0)
                {
                    CompletionPercentage = Math.Round((double)CompletedTasksCount / TotalTasksCount * 100, 2);
                }

                // 8. Kunin ang Recent Completed Tasks
                RecentCompletedTasks = allTasks
                    .Where(t => t.Status == "Completed" && t.CompletionDate.HasValue)
                    .OrderByDescending(t => t.CompletionDate)
                    .Take(5)
                    .ToList();
            }
        }
    }
}