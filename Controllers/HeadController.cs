using CMS.Models;
using CMS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CMS.Controllers
{
    [Authorize(Roles = "DeptHead")]
    public class HeadController : Controller
    {
        private readonly MongoDbContext _context;

        public HeadController(MongoDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userId = User.FindFirstValue("UserId");
            var complaints = await _context.Complaints
                .Find(c => c.AssignedToId == userId)
                .SortByDescending(c => c.CreatedDate)
                .ToListAsync();

            ViewBag.Total = complaints.Count;
            ViewBag.Pending = complaints.Count(c => c.Status == "Assigned"); // DH sees "Assigned" as their pending
            ViewBag.InProgress = complaints.Count(c => c.Status == "In Progress");
            ViewBag.Resolved = complaints.Count(c => c.Status == "Resolved");

            return View(complaints);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(string id, string status)
        {
            var userId = User.FindFirstValue("UserId");
            var filter = Builders<Complaint>.Filter.And(
                Builders<Complaint>.Filter.Eq(c => c.Id, id),
                Builders<Complaint>.Filter.Eq(c => c.AssignedToId, userId)
            );
            var update = Builders<Complaint>.Update.Set(c => c.Status, status);
            
            var result = await _context.Complaints.UpdateOneAsync(filter, update);

            if (result.ModifiedCount > 0)
                return Ok(new { success = true });

            return BadRequest(new { success = false, message = "Could not update status or unauthorized." });
        }
    }
}
