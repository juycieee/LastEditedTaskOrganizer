// TaskOrganizer.Services/AdminService.cs

using Microsoft.Extensions.Options;
using MongoDB.Driver;
using TaskOrganizer.Models;
// Tanggalin ang 'using System.Threading.Tasks;' para maiwasan ang conflict,
// at gagamitin natin ang buong namespace sa baba.

namespace TaskOrganizer.Services
{
    public class AdminService
    {
        private readonly IMongoCollection<Admin> _admins;

        // I-a-assume na tama na ang MongoDB setup mo dito
        public AdminService(IMongoDatabase database)
        {
            // Tanggalin ang pag-configure ng client/database. Diretso na sa pagkuha ng collection.
            _admins = database.GetCollection<Admin>("Admins");
        }
        // 1. GetByEmailAsync: Gamitin ang explicit System.Threading.Tasks.Task
        public async System.Threading.Tasks.Task<Admin?> GetByEmailAsync(string email)
        {
            return await _admins.Find(a => a.Email == email).FirstOrDefaultAsync();
        }

        // 2. CreateAsync: Gamitin ang explicit System.Threading.Tasks.Task
        // Ito ang nagko-cause ng "not all code paths return a value" error dahil sa ambiguity.
        public async System.Threading.Tasks.Task CreateAsync(Admin admin)
        {
            await _admins.InsertOneAsync(admin);
            // Hindi kailangan ng return statement dahil ang return type ay System.Threading.Tasks.Task (void)
        }

        // 3. ValidateLogin: Gamitin ang explicit System.Threading.Tasks.Task
        public async System.Threading.Tasks.Task<bool> ValidateLogin(string email, string passwordHash)
        {
            var admin = await _admins.Find(a => a.Email == email && a.PasswordHash == passwordHash)
                                     .FirstOrDefaultAsync();
            return admin != null;
        }
    }
}