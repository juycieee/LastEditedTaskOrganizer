using MongoDB.Driver;
using TaskOrganizer.Models;
using System.Collections.Generic; // ❗ ADDED ❗

namespace TaskOrganizer.Services
{
    public class AdminService
    {
        private readonly IMongoCollection<Admin> _admins;

        public AdminService(IMongoDatabase database)
        {
            // Pagkuha ng 'Admins' collection mula sa MongoDB
            _admins = database.GetCollection<Admin>("Admins");
        }

        // ❗ ADDED: Method para kunin ang LAHAT ng Admins ❗
        public async System.Threading.Tasks.Task<List<Admin>> GetAllAdminsAsync()
        {
            return await _admins.Find(_ => true).ToListAsync();
        }

        // ✅ IDINAGDAG: Method para kunin ang Admin object (Ito ang nagreresolve sa 'GetValidAdmin' error)
        public async System.Threading.Tasks.Task<Admin?> GetValidAdmin(string email, string passwordHash)
        {
            // Maghahanap ng Admin na may tugmang email at password hash
            return await _admins
                .Find(admin => admin.Email == email && admin.PasswordHash == passwordHash)
                .FirstOrDefaultAsync();
        }

        // 1. GetByEmailAsync: 
        public async System.Threading.Tasks.Task<Admin?> GetByEmailAsync(string email)
        {
            return await _admins.Find(a => a.Email == email).FirstOrDefaultAsync();
        }

        // 2. CreateAsync: 
        public async System.Threading.Tasks.Task CreateAsync(Admin admin)
        {
            await _admins.InsertOneAsync(admin);
        }

        // 3. ValidateLogin: Inayos para gamitin ang GetValidAdmin
        public async System.Threading.Tasks.Task<bool> ValidateLogin(string email, string passwordHash)
        {
            var admin = await GetValidAdmin(email, passwordHash);
            return admin != null;
        }
    }
}