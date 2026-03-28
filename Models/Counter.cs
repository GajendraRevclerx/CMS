using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CMS.Models
{
    public class Counter
    {
        [BsonId]
        public string Id { get; set; } = string.Empty; // e.g., "complaint_seq"

        [BsonElement("seq_value")]
        public long SequenceValue { get; set; }
    }
}
