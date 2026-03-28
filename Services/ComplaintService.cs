using CMS.Models;
using MongoDB.Driver;
using System;
using System.Threading.Tasks;

namespace CMS.Services
{
    public class ComplaintService
    {
        private readonly MongoDbContext _context;

        public ComplaintService(MongoDbContext context)
        {
            _context = context;
        }

        public async Task<long> GetNextSequenceValueAsync(string sequenceName)
        {
            var filter = Builders<Counter>.Filter.Eq(c => c.Id, sequenceName);
            var update = Builders<Counter>.Update.Inc(c => c.SequenceValue, 1);
            var options = new FindOneAndUpdateOptions<Counter>
            {
                ReturnDocument = ReturnDocument.After,
                IsUpsert = true
            };

            var result = await _context.Counters.FindOneAndUpdateAsync(filter, update, options);
            return result.SequenceValue;
        }

        public async Task CreateComplaintAsync(Complaint complaint)
        {
            var sequence = await GetNextSequenceValueAsync("complaint_seq");
            
            // Format: [StateCode]/[DeptCode]/[Year]/[Sequence]
            // We assume State and Department logic mapping passes the codes
            var year = DateTime.UtcNow.Year;
            
            // Just map some basic defaults if not provided to avoid crash
            var stateCode = !string.IsNullOrEmpty(complaint.State) ? complaint.State.Substring(0, 3).ToUpper() : "XXX";
            var deptCode = !string.IsNullOrEmpty(complaint.Department) ? complaint.Department.Substring(0, 3).ToUpper() : "XXX";
            
            // In a real scenario, you'd match the Codes from Master table.
            // Our seed creates Code="ELE" etc, let's assume the frontend passes the Code directly.
            
            complaint.ComplaintNo = $"{complaint.State}/{complaint.Department}/{year}/{sequence:D4}";
            complaint.CreatedDate = DateTime.UtcNow;
            complaint.Status = "Pending";

            await _context.Complaints.InsertOneAsync(complaint);
        }
    }
}
