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

        public async Task<string> GetNextComplaintIdAsync()
        {
            var filter = Builders<Counter>.Filter.Eq(c => c.Id, "complaint_seq");
            var result = await _context.Counters.Find(filter).FirstOrDefaultAsync();
            var sequence = (result?.SequenceValue ?? 0) + 1;
            return $"{DateTime.UtcNow.Year}/{sequence:D4}";
        }
 
        public async Task CreateComplaintAsync(Complaint complaint)
        {
            var sequence = await GetNextSequenceValueAsync("complaint_seq");
            var year = DateTime.UtcNow.Year;
            
            // Format: [Year]/[Sequence] (e.g., 2026/0001)
            complaint.ComplaintNo = $"{year}/{sequence:D4}";
            complaint.CreatedDate = DateTime.UtcNow;
            complaint.Status = string.IsNullOrEmpty(complaint.AssignedToId) ? "Pending" : "Assigned";
 
            await _context.Complaints.InsertOneAsync(complaint);
        }
    }
}
