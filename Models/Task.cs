using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System; // Idinagdag para sa DateTime

namespace TaskOrganizer.Models
{
    public class Task
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string? EmployeeId { get; set; } // Sino ang assigned
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Priority { get; set; }
        public DateTime? DueDate { get; set; }

        // ✅ IDINAGDAG: ID ng Admin/User na nag-assign ng Task
        public string? AssignerId { get; set; }

        public string? Status { get; set; }

        public DateTime DateAssigned { get; set; }

        public DateTime? CompletionDate { get; set; } // Pwedeng null kung hindi pa tapos ang task
    }
}