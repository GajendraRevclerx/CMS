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

        // Pending, Assigned, Resolved
        public string Status { get; set; } = "Pending";

        // Assignment Fields
        public string? AssignedToOfficerId { get; set; }
        public string? AssignedToOfficerName { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string Street { get; set; } = string.Empty;
        public string Locality { get; set; } = string.Empty;
        public string PinCode { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Site { get; set; } = string.Empty;
        public string IncidentDate { get; set; } = string.Empty;
        public string? EvidencePath { get; set; }
    }
}
