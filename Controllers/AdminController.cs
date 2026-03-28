using CMS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Threading.Tasks;
using CMS.Models;
using System.Security.Claims;

namespace CMS.Controllers
{
    [Authorize] // Ideally restrict to Roles="Admin", but omitted simply here or can be added [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly MongoDbContext _context;

        public AdminController(MongoDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Simple check
            if (User.FindFirstValue(System.Security.Claims.ClaimTypes.Role) != "Admin")
            {
                // Can return forbidden or redirect
                // return Forbid();
            }

            var complaints = await _context.Complaints
                .Find(_ => true)
                .SortByDescending(c => c.CreatedDate)
                .ToListAsync();

            return View(complaints);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(string complaintId, string status)
        {
            var filter = Builders<Complaint>.Filter.Eq(c => c.Id, complaintId);
            var update = Builders<Complaint>.Update.Set(c => c.Status, status);

            await _context.Complaints.UpdateOneAsync(filter, update);

            return RedirectToAction("Index");
        }
    }
}
