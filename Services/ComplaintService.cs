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
            var year = DateTime.UtcNow.Year;
            
            // In a real scenario, you'd match the Codes from Master table.
            // Our seed creates Code="ELE" etc, let's assume the frontend passes the Code directly.
            
            complaint.ComplaintNo = $"{year}/{sequence:D5}";
            complaint.CreatedDate = DateTime.UtcNow;
            complaint.Status = "Pending";

            await _context.Complaints.InsertOneAsync(complaint);
        }
    }
}
