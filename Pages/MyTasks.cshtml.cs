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
// ❗ FIX: Inilipat ang alias directive dito, sa laba ng namespace ❗
using AppTask = TaskOrganizer.Models.Task;

namespace TaskOrganizer.Pages
{
    // ❗ RECOMMENDATION: Siguraduhin na Authorized user lang ang makaka-access nito ❗
    [Authorize(Roles = "Employee")]

    // using AppTask = TaskOrganizer.Models.Task; <-- TINANGGAL DITO

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
        public int HighPriorityTasksCount => AllMyTasks.Count(t => t.Status != "Completed" && t.Priority == "High");

        // Ito ang gagamitin sa task card view
        public IList<AppTask> CurrentPendingTasks { get; set; } = new List<AppTask>();

        // Employee Map (Id -> Name)
        public Dictionary<string, string> EmployeeIdToNameMap { get; set; } = new Dictionary<string, string>();

        public MyTasksModel(TaskService taskService, EmployeeServices employeeService)
        {
            _taskService = taskService;
            _employeeService = employeeService;
        }

        public async System.Threading.Tasks.Task OnGetAsync()
        {
            // ❗ CRITICAL FIX: Kunin ang ID gamit ang custom claim na "EmployeeId" ❗
            string? currentEmployeeId = User.FindFirstValue("EmployeeId");

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

                // 1. Kukunin ang LAHAT ng Tasks para sa current user (gamit ang ID)
                // *** TANDAAN: Ang bawat Task sa AllMyTasks ay naglalaman na ng buong DateTime
                // *** (Petsa at Oras) sa DueDate property, kung ito ay ni-set ng Admin.
                AllMyTasks = await _taskService.GetTasksByEmployeeIdAsync(currentEmployeeId);

                // 2. I-filter ang mga tasks na ipapakita sa card list
                CurrentPendingTasks = AllMyTasks
                    .Where(t => t.Status != "Completed" && t.Status != "Archived")
                    .ToList();

                // OPTIONAL: Maaari mo ring i-sort ang pending tasks ayon sa DueDate at DueTime
                // para mas mauna ang malapit na ma-due.
                CurrentPendingTasks = CurrentPendingTasks
                    .OrderBy(t => t.DueDate.HasValue) // Para unahin ang may petsa
                    .ThenBy(t => t.DueDate)            // At i-sort ayon sa DueDate/DueTime
                    .ToList();


                // 3. I-fetch ang LAHAT ng Employees at i-populate ang Map
                // Ito ay ginagamit para i-display ang pangalan ng nag-assign (kung galing sa Admin side)
                var allEmployees = await _employeeService.GetAllEmployeesAsync();
                EmployeeIdToNameMap = allEmployees.ToDictionary(e => e.Id!, e => e.Name);

                // Walang return statement sa dulo, kaya walang 'not all code paths return a value' error.
            }
        }

        // Action para sa "Mark as Done"
        public async Task<IActionResult> OnPostMarkAsCompleteAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToPage();
            }

            // I-update ang status ng task sa "Completed"
            await _taskService.UpdateTaskStatusAsync(id, "Completed");

            // I-redirect pabalik sa parehong page para mag-refresh
            return RedirectToPage();
        }
    }
}