using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace CMS.Models
{
    [BsonIgnoreExtraElements]
    public class Complaint
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        // E.g. CHD/ELE/2025/0001
        public string ComplaintNo { get; set; } = string.Empty;

        public string UserId { get; set; } = string.Empty; // MobileNo of the user
        public string? RegisteredById { get; set; } // Staff member who registered it

        // Citizen Contact Info
        public string? FullName { get; set; }
        public string? Email { get; set; }

        // Form Fields
        public string? ComplaintTitle { get; set; }
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

        // Pending, Assigned, Resolved
        public string Status { get; set; } = "Pending";

        public string? ResolutionRemark { get; set; }

        public string? Area { get; set; }
        public string? AreaOfJurisdiction { get; set; }
        
        // Assigned to a specific Department Head (User Id)
        public string? AssignedToId { get; set; }
        public string? AssignedToName { get; set; }
        public string? AssignedToMobile { get; set; }
        public string? Division { get; set; }
        public string? SubDivision { get; set; }

        public string? AttachmentPath { get; set; }
        public DateTime? ResolutionDate { get; set; }
        public double Rating { get; set; } = 0;
        public string Priority { get; set; } = "Medium";

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
