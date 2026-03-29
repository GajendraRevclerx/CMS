using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace CMS.Models
{
    public class Complaint
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        // E.g. CHD/ELE/2025/0001
        public string ComplaintNo { get; set; } = string.Empty;

        public string UserId { get; set; } = string.Empty; // MobileNo of the user

        // Form Fields
        public string ComplaintTitle { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        
        public string? Street { get; set; }
        public string? Locality { get; set; }
        public string? PinCode { get; set; }
        public string? Site { get; set; }
        public string? Source { get; set; }
        public DateTime? IncidentDate { get; set; }

        // Pending, Assigned, In Progress, Resolved
        public string Status { get; set; } = "Pending";

        public string? Area { get; set; }
        
        // Assigned to a specific Department Head (User Id)
        public string? AssignedToId { get; set; }
        public string? AssignedToName { get; set; }

        public string? AttachmentPath { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
