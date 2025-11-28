using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using TaskOrganizer.Services;
using TaskOrganizer.Models;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using System;
using AppTask = TaskOrganizer.Models.Task;

namespace TaskOrganizer.Pages
{
    [Authorize(Roles = "Employee")]
    public class CalendarModel : PageModel
    {
        private readonly TaskService _taskService;

        public string EmployeeUsername { get; set; } = "User";
        public IList<AppTask> AllMyTasks { get; set; } = new List<AppTask>();
        public IList<AppTask> TodaysTasks { get; set; } = new List<AppTask>();

        public int TotalTasksCount => AllMyTasks.Count;
        public int CompletedTasksCount => AllMyTasks.Count(t => t.Status == "Completed");
        public int PendingTasksCount => AllMyTasks.Count(t => t.Status == "Pending" || t.Status == "In Progress");
        public int HighPriorityTasksCount => AllMyTasks.Count(t => t.Status != "Completed" && t.Priority == "High");

        public CalendarModel(TaskService taskService)
        {
            _taskService = taskService;
        }

        // Helper Method para sa Time Zone Conversion
        private TimeZoneInfo GetPhilippineTimeZone()
        {
            try
            {
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

        private void ConvertTasksToPhTime(IList<AppTask> tasks, TimeZoneInfo phTimeZone)
        {
            foreach (var task in tasks)
            {
                if (task.DueDate.HasValue)
                {
                    DateTime utcTime = task.DueDate.Value;
                    if (task.DueDate.Value.Kind != DateTimeKind.Utc)
                    {
                        utcTime = DateTime.SpecifyKind(task.DueDate.Value, DateTimeKind.Utc);
                    }
                    task.DueDate = TimeZoneInfo.ConvertTimeFromUtc(utcTime, phTimeZone);
                }
            }
        }

        public async System.Threading.Tasks.Task OnGetAsync()
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated) return;

            EmployeeUsername = User.Identity.Name ?? "Employee";
            string? currentEmployeeId = User.FindFirstValue("EmployeeId");

            if (string.IsNullOrEmpty(currentEmployeeId)) return;

            AllMyTasks = await _taskService.GetTasksByEmployeeIdAsync(currentEmployeeId);

            var phTimeZone = GetPhilippineTimeZone();
            ConvertTasksToPhTime(AllMyTasks, phTimeZone); // I-convert ang lahat ng tasks

            var phTodayDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, phTimeZone).Date;

            // Kukunin ang tasks para sa ARAW na ITO, hindi completed, naka-sort sa DueTime
            TodaysTasks = AllMyTasks
                .Where(t =>
                    t.DueDate.HasValue &&
                    t.DueDate.Value.Date == phTodayDate &&
                    t.Status != "Completed"
                )
                .OrderBy(t => t.DueDate)
                .ToList();
        }

        // ==============================================================================
        // ❗ BAGONG HANDLER METHOD PARA SA PAG-CLICK NG PETSA ❗
        // ==============================================================================
        public async System.Threading.Tasks.Task<JsonResult> OnGetTasksByDateAsync(DateTime date)
        {
            // Security Check
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return new JsonResult(new { success = false, message = "Not authenticated" });
            }

            string? currentEmployeeId = User.FindFirstValue("EmployeeId");
            if (string.IsNullOrEmpty(currentEmployeeId))
            {
                return new JsonResult(new { success = false, message = "Employee ID not found" });
            }

            // Kukunin ang lahat ng tasks ng user
            var allTasks = await _taskService.GetTasksByEmployeeIdAsync(currentEmployeeId);

            // I-convert ang mga petsa sa PH Time
            var phTimeZone = GetPhilippineTimeZone();
            ConvertTasksToPhTime(allTasks, phTimeZone);

            // I-filter ang tasks para sa specific na petsa, at hindi pa completed
            var filteredTasks = allTasks
                .Where(t =>
                    t.DueDate.HasValue &&
                    t.DueDate.Value.Date == date.Date && // Compare lang ang date part
                    t.Status != "Completed"
                )
                .OrderBy(t => t.DueDate) // I-sort sa oras
                .Select(t => new
                {
                    t.Title,
                    t.Description,
                    t.Priority,
                    // Format ang oras para madaling gamitin sa JavaScript
                    DueDate = t.DueDate.HasValue ? t.DueDate.Value.ToString("hh:mm tt") : "Any Time"
                })
                .ToList();

            // Ibabalik ang tasks bilang JSON
            return new JsonResult(filteredTasks);
        }
    }
}