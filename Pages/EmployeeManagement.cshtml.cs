// EmployeeManagement.cshtml.cs

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TaskOrganizer.Models;
using TaskOrganizer.Services; // Kailangan para sa 'EmployeeServices'
using System.Collections.Generic;
using System.Threading.Tasks; // Kailangan para sa 'Task'

namespace TaskOrganizer.Pages
{
    public class EmployeeManagementModel : PageModel
    {
        // 1. I-DECLARE ang service gamit ang TAMANG CLASS NAME
        private readonly EmployeeServices _employeeService; // ❗ Dapat 'EmployeeServices' (with 's') ❗

        // 2. I-INJECT ang service sa constructor gamit ang TAMANG CLASS NAME
        public EmployeeManagementModel(EmployeeServices employeeService) // ❗ Dapat 'EmployeeServices' (with 's') ❗
        {
            _employeeService = employeeService;
        }

        // 3. I-DECLARE ang property para i-hold ang listahan ng employees
        public IList<Employee> Employees { get; set; } = new List<Employee>();

        // 4. OnGet() method - ginagamit ang System.Threading.Tasks.Task para maiwasan ang ambiguity
        // Ito ay magbabalik ng 'void' kaya hindi na kailangan ng return statement.
        public async System.Threading.Tasks.Task OnGetAsync()
        {
            // Tiyakin na ang method name sa EmployeeServices ay 'GetAllEmployeesAsync()'
            // Kung ang method mo ay 'GetEmployeesAsync()', gamitin 'yan, pero karaniwan ang 'GetAll...'

            // I-assume ko na 'GetAllEmployeesAsync()' ang method name, tulad ng ginawa natin sa AssignTask.cs
            Employees = await _employeeService.GetAllEmployeesAsync();

            // Ang method na ito ay 'async Task', kaya hindi na kailangan ng return statement dito.
        }
    }
}