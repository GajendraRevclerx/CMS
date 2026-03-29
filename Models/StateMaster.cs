using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CMS.Models
{
    public class StateMaster
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string ShortCode { get; set; } = string.Empty;
    }
}
