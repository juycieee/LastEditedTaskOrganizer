using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TaskOrganizer.Models
{
    public class Task
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string EmployeeId { get; set; } // Sino ang assigned
        public string Title { get; set; }
        public string Description { get; set; }
        public string Priority { get; set; }
        public DateTime? DueDate { get; set; }


        // REQUIRED: Idagdag ang Status (para sa Completed/Pending)
        public string Status { get; set; }

        // REQUIRED: Idagdag ang DateAssigned (tulad ng nasa TaskService mo)
        public DateTime DateAssigned { get; set; }

        // ❗ SOLUTION: IDAGDAG ANG COMPLETIONDATE PROPERTY ❗
        public DateTime? CompletionDate { get; set; } // Pwedeng null kung hindi pa tapos ang task
    }
}