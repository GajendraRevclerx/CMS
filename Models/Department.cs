using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CMS.Models
{
    public class Department
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string HeadName { get; set; } = string.Empty;
        public string Division { get; set; } = string.Empty;
        public int SlaDays { get; set; } = 7;
        public string Email { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";

        // Optional: Emoji for UI
        public string Icon { get; set; } = "💡";
    }
}
