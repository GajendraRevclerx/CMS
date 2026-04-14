using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace CMS.Models
{
    [BsonIgnoreExtraElements]
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

        // Citizen, DeptHead, Admin
        public string Role { get; set; } = "Citizen";

        // For DeptHead role
        public string? Department { get; set; }
        public string? Division { get; set; }
        public string? SubDivision { get; set; }
        public string? Designation { get; set; }
        public string? Area { get; set; }
        public string? AreaOfJurisdiction { get; set; }
        public string? Landline { get; set; }
    }
}
