using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace CMS.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        // Ensure this is uniquely indexed in DB context
        public string MobileNo { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Role { get; set; } = "Citizen"; // Valid values: "Citizen", "Admin", "Officer"

        // Officer Specific Fields
        public string Designation { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty; // Department Name or ID
        public string AreaOfJurisdiction { get; set; } = string.Empty;
        public string Landline { get; set; } = string.Empty;
    }
}
