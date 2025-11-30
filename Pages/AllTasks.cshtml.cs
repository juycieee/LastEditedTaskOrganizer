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
        // Ipagpalagay na ang TaskService ay mayroong 'DeleteTaskAsync' method na kailangan.
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
            // Ipagpalagay na ang 'Status' na "Archived" ay ginagamit pa rin for filtering.
            filteredTasks = filteredTasks.Where(t => t.Status != "Archived");

            // Filter by Priority
            if (!string.IsNullOrEmpty(PriorityFilter))
            {
                filteredTasks = filteredTasks.Where(t => t.Priority == PriorityFilter);
            }

            // Filter by Employee
            if (!string.IsNullOrEmpty(EmployeeFilter))
            {
                // Note: Ipagpalagay na ang EmployeeFilter ay Employee Name, kailangan mo ng 'lookup' dito.
                // Base sa inyong .cshtml, mukhang ang EmployeeFilter ay Employee Name.
                // Kung ang EmployeeFilter ay EmployeeId, ayos lang ang code sa baba.
                // Kung Employee Name ang dala, kailangan ng Employee List lookup.
                // Pero sa ngayon, ipagpapalagay ko na ang property t.EmployeeId sa Task model ay ang kailangan i-match.
                // Kung ang EmployeeFilter ay ang ID, ito ay tama.
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

        // 🚨 BINAGO ITO: Permanenteng burahin ang task sa database. 🚨
        public async Task<IActionResult> OnPostDeleteAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToPage();
            }

            // ❗ KRITIKAL NA PAGBABAGO: Imbes na i-update ang status sa "Archived",
            // TULOY na itong BURAHIN sa database gamit ang TaskService.

            // Ipagpalagay na mayroong 'DeleteTaskAsync' method ang inyong TaskService
            await _taskService.DeleteTaskAsync(id); // <--- ITO ANG MAGBUBURA NG PERMANENTE

            // I-redirect pabalik sa AllTasks page
            return RedirectToPage();
        }
    }
}