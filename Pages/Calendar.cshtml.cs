using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims; // Kailangan para sa claims
using TaskOrganizer.Services; // Kailangan para sa TaskService
using TaskOrganizer.Models; // Kailangan para sa Task Model
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization; // Para i-secure ang page
using System; // Kailangan para sa DateTime.Today
using AppTask = TaskOrganizer.Models.Task;

namespace TaskOrganizer.Pages
{
    [Authorize(Roles = "Employee")]

    public class CalendarModel : PageModel
    {
        private readonly TaskService _taskService;

        // ❗ IDINAGDAG: Properties para sa data at counts ❗
        public string EmployeeUsername { get; set; } = "User";
        public IList<AppTask> AllMyTasks { get; set; } = new List<AppTask>();

        // ❗ BAGONG PROPERTY: Para sa listahan ng tasks na due TODAY ❗
        public IList<AppTask> TodaysTasks { get; set; } = new List<AppTask>();

        // Properties para sa Summary Counts (Gaya ng sa Dashboard at MyTasks)
        public int TotalTasksCount => AllMyTasks.Count;
        public int CompletedTasksCount => AllMyTasks.Count(t => t.Status == "Completed");
        public int PendingTasksCount => AllMyTasks.Count(t => t.Status == "Pending" || t.Status == "In Progress");
        public int HighPriorityTasksCount => AllMyTasks.Count(t => t.Status != "Completed" && t.Priority == "High");

        public CalendarModel(TaskService taskService)
        {
            _taskService = taskService;
        }

        public async System.Threading.Tasks.Task OnGetAsync()
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                // Kung walang user, huwag mag-proceed
                return;
            }

            // 1. KUNIN ANG USERNAME
            EmployeeUsername = User.Identity.Name ?? "Employee";

            // 2. KUNIN ANG EMPLOYEE ID (Galing sa custom claim na "EmployeeId")
            string? currentEmployeeId = User.FindFirstValue("EmployeeId");

            if (string.IsNullOrEmpty(currentEmployeeId))
            {
                return;
            }

            // 3. Kukunin ang LAHAT ng Tasks para sa current user (gamit ang ID)
            // TANDAAN: Ang DueDate property ay naglalaman na ng Petsa AT Oras
            AllMyTasks = await _taskService.GetTasksByEmployeeIdAsync(currentEmployeeId);

            // ❗ 4. UPDATE: FILTER AT SORT ANG TASKS NA DUE NGAYON (KASAMA ANG ORAS) ❗
            TodaysTasks = AllMyTasks
                .Where(t =>
                    t.DueDate.HasValue &&
                    t.DueDate.Value.Date == DateTime.Today.Date && // Tinitingnan kung today ang date
                    t.Status != "Completed" // Hindi na isasama ang tapos na
                )
                // ❗ BAGONG LOGIC: I-sort ang TodaysTasks ayon sa Due Time (ascendinG) ❗
                .OrderBy(t => t.DueDate)
                .ToList();

            // Ang counts (TotalTasksCount, etc.) ay awtomatikong magka-calculate
            // dahil sa pag-set ng AllMyTasks.
        }
    }
}