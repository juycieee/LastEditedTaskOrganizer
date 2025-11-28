using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskOrganizer.Models;
using TaskOrganizer.Services;
using System.Security.Claims;
using System;
using Microsoft.AspNetCore.Authorization;
using AppTask = TaskOrganizer.Models.Task;

namespace TaskOrganizer.Pages
{
    [Authorize(Roles = "Employee")]
    public class MyTasksModel : PageModel
    {
        private readonly TaskService _taskService;
        private readonly EmployeeServices _employeeService;

        // Properties para sa Card Counts at Listahan
        public IList<AppTask> AllMyTasks { get; set; } = new List<AppTask>();
        public string EmployeeUsername { get; set; } = "User";

        public int TotalTasksCount => AllMyTasks.Count;
        public int CompletedTasksCount => AllMyTasks.Count(t => t.Status == "Completed");
        public int PendingTasksCount => AllMyTasks.Count(t => t.Status == "Pending" || t.Status == "In Progress");

        // ❗ UPDATED PROPERTY: Para sa Missing/Overdue Tasks Count ❗
        public int OverdueTasksCount { get; set; } = 0;

        // High Priority count, kung kailangan pa
        public int HighPriorityTasksCount => AllMyTasks.Count(t => t.Status != "Completed" && t.Priority == "High");

        // Ito ang gagamitin sa task card view
        public IList<AppTask> CurrentPendingTasks { get; set; } = new List<AppTask>();

        // Para i-store ang computed status (ID -> Status: "Overdue" o "On Time")
        public Dictionary<string, string> TaskStatuses { get; set; } = new Dictionary<string, string>();

        // Employee Map (Id -> Name).
        public Dictionary<string, string> EmployeeIdToNameMap { get; set; } = new Dictionary<string, string>();

        public MyTasksModel(TaskService taskService, EmployeeServices employeeService)
        {
            _taskService = taskService;
            _employeeService = employeeService;
        }

        // Helper Method para sa Time Zone Conversion
        private TimeZoneInfo GetPhilippineTimeZone()
        {
            try
            {
                // Gamitin ang standard ID (Windows at Linux/macOS)
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
            string? currentEmployeeId = User.FindFirstValue("EmployeeId");

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                EmployeeUsername = User.Identity.Name ?? "Employee";

                if (string.IsNullOrEmpty(currentEmployeeId)) return;

                var allEmployees = await _employeeService.GetAllEmployeesAsync();
                EmployeeIdToNameMap = allEmployees.ToDictionary(e => e.Id!, e => e.Name);

                AllMyTasks = await _taskService.GetTasksByEmployeeIdAsync(currentEmployeeId);

                TimeZoneInfo phTimeZone = GetPhilippineTimeZone();
                DateTime phNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, phTimeZone);

                int overdueCounter = 0; // Counter para sa Overdue Tasks

                CurrentPendingTasks = AllMyTasks
                    .Where(t => t.Status != "Completed" && t.Status != "Archived")
                    .Select(t =>
                    {
                        // 1. SOLVED: Gumawa ng Bagong AppTask Object (Safe Cloning)
                        var displayTask = new AppTask
                        {
                            Id = t.Id,
                            Title = t.Title,
                            Description = t.Description,
                            Priority = t.Priority,
                            Status = t.Status,
                            EmployeeId = t.EmployeeId,
                            AssignerId = t.AssignerId,
                            DueDate = t.DueDate
                        };

                        // 2. DUE DATE CONVERSION AND OVERDUE CHECK
                        if (displayTask.DueDate.HasValue)
                        {
                            DateTime utcTime = displayTask.DueDate.Value;

                            // A: Siguraduhin na ang Kind ay UTC
                            if (displayTask.DueDate.Value.Kind != DateTimeKind.Utc)
                            {
                                utcTime = DateTime.SpecifyKind(displayTask.DueDate.Value, DateTimeKind.Utc);
                            }

                            // B: I-convert at I-ASSIGN ang converted time sa bagong object
                            displayTask.DueDate = TimeZoneInfo.ConvertTimeFromUtc(utcTime, phTimeZone);

                            // C: I-check kung overdue based sa converted PH Time
                            if (displayTask.DueDate.Value < phNow)
                            {
                                TaskStatuses[displayTask.Id!] = "Overdue";
                                overdueCounter++; // Increment ang counter
                            }
                            else
                            {
                                TaskStatuses[displayTask.Id!] = "On Time";
                            }
                        }
                        else
                        {
                            TaskStatuses[displayTask.Id!] = "On Time";
                        }

                        return displayTask;
                    })
                    .OrderBy(t => t.DueDate.HasValue)
                    .ThenBy(t => t.DueDate)
                    .ToList();

                // ❗ I-set ang final count para sa Summary Card ❗
                OverdueTasksCount = overdueCounter;
            }
        }

        // Action para sa "Mark as Done"
        public async Task<IActionResult> OnPostMarkAsCompleteAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToPage();
            }

            await _taskService.UpdateTaskStatusAsync(id, "Completed");

            return RedirectToPage();
        }
    }
}