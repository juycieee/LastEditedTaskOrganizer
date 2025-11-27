using MongoDB.Driver;
using TaskOrganizer.Models;
using System.Collections.Generic;
// Hindi kailangan ng using System.Threading.Tasks; kung gagamit ng explicit System.Threading.Tasks.Task

namespace TaskOrganizer.Services
{
    public class EmployeeServices
    {
        private readonly IMongoCollection<Employee> _employeeCollection;

        public EmployeeServices(IMongoDatabase database)
        {
            _employeeCollection = database.GetCollection<Employee>("Employees");
        }

        // --- AUTHENTICATION/REGISTER METHODS ---

        // 1. 🔍 GetByEmailAsync
        public async System.Threading.Tasks.Task<Employee> GetByEmailAsync(string email)
        {
            return await _employeeCollection.Find(e => e.Email == email).FirstOrDefaultAsync();
        }

        // 2. ➕ CreateAsync
        public async System.Threading.Tasks.Task CreateAsync(Employee newEmployee)
        {
            await _employeeCollection.InsertOneAsync(newEmployee);
        }

        // 3. 🔑 GetValidEmployee (NEW/MODIFIED LOGIC FOR LOGIN CLAIMS)
        /// <summary>
        /// Attempts to find and return a valid Employee object based on email and password hash.
        /// </summary>
        /// <param name="email">The employee's login email.</param>
        /// <param name="passwordHash">The hashed password.</param>
        /// <returns>The Employee object if found and valid, otherwise null.</returns>
        public async System.Threading.Tasks.Task<Employee?> GetValidEmployee(string email, string passwordHash)
        {
            var filter = Builders<Employee>.Filter.Eq(e => e.Email, email) &
                         Builders<Employee>.Filter.Eq(e => e.PasswordHash, passwordHash);

            // Nagbabalik ng Employee object (na may Id at Name) o null kung hindi nag-match
            return await _employeeCollection.Find(filter).FirstOrDefaultAsync();
        }

        // 4. 📚 GetAllEmployeesAsync
        public async System.Threading.Tasks.Task<List<Employee>> GetAllEmployeesAsync()
        {
            return await _employeeCollection.Find(_ => true).ToListAsync();
        }
    }
}