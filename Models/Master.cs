using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace CMS.Models
{
    public class Master
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        public List<StateData> States { get; set; } = new List<StateData>();
        public List<DepartmentMaster> Departments { get; set; } = new List<DepartmentMaster>();
    }

    public class StateData
    {
        public string FullName { get; set; } = string.Empty;
        public string ShortCode { get; set; } = string.Empty;
        public List<string> Cities { get; set; } = new List<string>();
    }

    public class DepartmentMaster
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
}
