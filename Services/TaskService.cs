using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskOrganizer.Models;
using System.Linq; // Added for clarity (though not strictly needed for this file)

namespace TaskOrganizer.Services
{
    // Alias para sa Task model (para mas malinis)
    using AppTask = TaskOrganizer.Models.Task;

    public class TaskService
    {
        private readonly IMongoCollection<AppTask> _taskCollection;

        public TaskService(IMongoDatabase database)
        {
            _taskCollection = database.GetCollection<AppTask>("Tasks");
        }

        public async System.Threading.Tasks.Task AssignNewTaskAsync(AppTask newTask)
        {
            newTask.DateAssigned = System.DateTime.Now;

            if (string.IsNullOrEmpty(newTask.Status))
            {
                newTask.Status = "Pending";
            }

            await _taskCollection.InsertOneAsync(newTask);
        }

        public async System.Threading.Tasks.Task<List<AppTask>> GetAllTasksAsync()
        {
            return await _taskCollection.Find(_ => true).ToListAsync();
        }

        // ❗ CRITICAL: Ito ang filtering method para sa bawat employee ❗
        public async System.Threading.Tasks.Task<List<AppTask>> GetTasksByEmployeeIdAsync(string employeeId)
        {
            // Tiyakin na ang field name sa Task model na Assigned sa employee ay 'EmployeeId'
            // Base sa iyong code: task.EmployeeId == employeeId
            return await _taskCollection
                .Find(task => task.EmployeeId == employeeId)
                .ToListAsync();
        }

        public async System.Threading.Tasks.Task UpdateTaskStatusAsync(string taskId, string newStatus)
        {
            var filter = Builders<AppTask>.Filter.Eq(t => t.Id, taskId);
            var update = Builders<AppTask>.Update.Set(t => t.Status, newStatus);

            if (newStatus == "Completed")
            {
                update = update.Set(t => t.CompletionDate, System.DateTime.Now);
            }

            await _taskCollection.UpdateOneAsync(filter, update);
        }
    }
}