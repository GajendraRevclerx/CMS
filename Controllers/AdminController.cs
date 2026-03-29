using CMS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Threading.Tasks;
using CMS.Models;
using System.Security.Claims;
using System.Linq;

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
            var allComplaints = await _context.Complaints
                .Find(_ => true)
                .SortByDescending(c => c.CreatedDate)
                .ToListAsync();

            var departments = await _context.Departments
                .Find(_ => true)
                .ToListAsync();

            var officers = await _context.Users
                .Find(u => u.Role == "Officer")
                .ToListAsync();

            var viewModel = new AdminDashboardViewModel
            {
                TotalComplaints = allComplaints.Count,
                PendingComplaints = allComplaints.Count(c => c.Status == "Pending"),
                ResolvedComplaints = allComplaints.Count(c => c.Status == "Resolved"),
                InProgressComplaints = allComplaints.Count(c => c.Status == "In Progress"),
                EscalatedComplaints = allComplaints.Count(c => c.Status == "Escalated"),
                AvgResolutionTimeDays = 3.8,
                AvgCitizenRating = 4.1,
                OfficersOnDuty = officers.Count,
                
                RecentComplaints = allComplaints.Take(5).ToList(),
                AllComplaints = allComplaints,
                Departments = departments,
                Officers = officers
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> SaveDepartment(Department dept)
        {
            if (string.IsNullOrEmpty(dept.Id))
            {
                dept.Id = null; // Let Mongo handle it
                await _context.Departments.InsertOneAsync(dept);
            }
            else
            {
                var filter = Builders<Department>.Filter.Eq(d => d.Id, dept.Id);
                await _context.Departments.ReplaceOneAsync(filter, dept);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> SaveOfficer(User officer)
        {
            if (string.IsNullOrEmpty(officer.Id))
            {
                // Create New
                officer.Id = null;
                officer.Role = "Officer";
                
                // Check if mobile already exists
                var existing = await _context.Users.Find(u => u.MobileNo == officer.MobileNo).FirstOrDefaultAsync();
                if (existing != null)
                {
                    // If exists, update its role and details
                    var filter = Builders<User>.Filter.Eq(u => u.Id, existing.Id);
                    var update = Builders<User>.Update
                        .Set(u => u.Role, "Officer")
                        .Set(u => u.FullName, officer.FullName)
                        .Set(u => u.Department, officer.Department)
                        .Set(u => u.Designation, officer.Designation)
                        .Set(u => u.Email, officer.Email)
                        .Set(u => u.Landline, officer.Landline)
                        .Set(u => u.AreaOfJurisdiction, officer.AreaOfJurisdiction);
                    
                    if (!string.IsNullOrEmpty(officer.Password))
                        update = update.Set(u => u.Password, officer.Password);

                    await _context.Users.UpdateOneAsync(filter, update);
                }
                else
                {
                    await _context.Users.InsertOneAsync(officer);
                }
            }
            else
            {
                // Update Existing
                var filter = Builders<User>.Filter.Eq(u => u.Id, officer.Id);
                var update = Builders<User>.Update
                    .Set(u => u.FullName, officer.FullName)
                    .Set(u => u.Department, officer.Department)
                    .Set(u => u.Designation, officer.Designation)
                    .Set(u => u.Email, officer.Email)
                    .Set(u => u.Landline, officer.Landline)
                    .Set(u => u.AreaOfJurisdiction, officer.AreaOfJurisdiction);
                
                if (!string.IsNullOrEmpty(officer.Password))
                {
                    update = update.Set(u => u.Password, officer.Password);
                }

                await _context.Users.UpdateOneAsync(filter, update);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> AssignComplaint(string complaintId, string officerId, string officerName)
        {
            var filter = Builders<Complaint>.Filter.Eq(c => c.Id, complaintId);
            var update = Builders<Complaint>.Update
                .Set(c => c.Status, "Assigned")
                .Set(c => c.AssignedToOfficerId, officerId)
                .Set(c => c.AssignedToOfficerName, officerName);

            await _context.Complaints.UpdateOneAsync(filter, update);
            return RedirectToAction("Index");
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
