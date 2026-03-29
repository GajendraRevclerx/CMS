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
    [Authorize(Roles = "Admin")]
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
            var complaints = await _context.Complaints
                .Find(_ => true)
                .SortByDescending(c => c.CreatedDate)
                .ToListAsync();

            ViewBag.Total = complaints.Count;
            ViewBag.Pending = complaints.Count(c => c.Status == "Pending");
            ViewBag.InProgress = complaints.Count(c => c.Status == "In Progress" || c.Status == "Assigned");
            // Unassigned specific to the new "Assignments" panel
            ViewBag.Unassigned = complaints.Count(c => string.IsNullOrEmpty(c.AssignedToId));
            ViewBag.Resolved = complaints.Count(c => c.Status == "Resolved");
            
            var now = System.DateTime.UtcNow;
            ViewBag.Escalated = complaints.Count(c => c.Status != "Resolved" && c.Status != "Closed" && (now - c.CreatedDate).TotalDays > 5);
            ViewBag.Notifications = complaints.Count(c => (now - c.CreatedDate).TotalDays <= 1);
            ViewBag.SLA = complaints.Count(c => c.Status != "Resolved" && c.Status != "Closed" && (now - c.CreatedDate).TotalDays > 3);

            // User Management Data
            ViewBag.Users = await _context.Users.Find(_ => true).ToListAsync();
            // All Officers
            ViewBag.Heads = await _context.Users.Find(u => u.Role == "DeptHead").ToListAsync();
            
            // Fetch Master Data for Departments
            var masters = await _context.Masters.Find(_ => true).FirstOrDefaultAsync();
            ViewBag.Masters = masters ?? new Master();

            return View(complaints);
        }

        [HttpPost]
        public async Task<IActionResult> EditUser(User user)
        {
            try
            {
                var filter = Builders<User>.Filter.Eq(u => u.Id, user.Id);
                var update = Builders<User>.Update
                    .Set(u => u.FullName, user.FullName)
                    .Set(u => u.MobileNo, user.MobileNo)
                    .Set(u => u.Email, user.Email)
                    .Set(u => u.Role, user.Role)
                    .Set(u => u.Department, user.Department)
                    .Set(u => u.Area, user.Area)
                    .Set(u => u.AreaOfJurisdiction, user.AreaOfJurisdiction)
                    .Set(u => u.Landline, user.Landline)
                    .Set(u => u.Designation, user.Designation);

                await _context.Users.UpdateOneAsync(filter, update);
                return Json(new { success = true });
            }
            catch (System.Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(User user)
        {
            try
            {
                await _context.Users.InsertOneAsync(user);
                return Json(new { success = true });
            }
            catch (System.Exception)
            {
                // Likely duplicate key error for MobileNo
                return Json(new { success = false, message = "Could not create user. Mobile number might already be registered." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddDepartment(DepartmentMaster dept)
        {
            var master = await _context.Masters.Find(_ => true).FirstOrDefaultAsync();
            if (master == null)
            {
                master = new Master();
                await _context.Masters.InsertOneAsync(master);
            }

            var filter = Builders<Master>.Filter.Eq(m => m.Id, master.Id);
            var update = Builders<Master>.Update.Push(m => m.Departments, dept);
            await _context.Masters.UpdateOneAsync(filter, update);

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> EditDepartment(string oldCode, DepartmentMaster dept)
        {
            var master = await _context.Masters.Find(_ => true).FirstOrDefaultAsync();
            if (master != null)
            {
                var existing = master.Departments.FirstOrDefault(d => d.Code == oldCode);
                if (existing != null)
                {
                    existing.Name = dept.Name;
                    existing.Code = dept.Code;
                    existing.HeadName = dept.HeadName;
                    existing.DivisionZone = dept.DivisionZone;
                    existing.SLADays = dept.SLADays;
                    existing.Icon = dept.Icon;
                    existing.Email = dept.Email;
                    existing.Status = dept.Status;

                    var filter = Builders<Master>.Filter.Eq(m => m.Id, master.Id);
                    var update = Builders<Master>.Update.Set(m => m.Departments, master.Departments);
                    await _context.Masters.UpdateOneAsync(filter, update);
                }
            }
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> Reassign(string complaintId, string headId, string headName)
        {
            var filter = Builders<Complaint>.Filter.Eq(c => c.Id, complaintId);
            var update = Builders<Complaint>.Update
                .Set(c => c.AssignedToId, headId)
                .Set(c => c.AssignedToName, headName)
                .Set(c => c.Status, "Assigned");

            await _context.Complaints.UpdateOneAsync(filter, update);
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(string complaintId, string status)
        {
            var filter = Builders<Complaint>.Filter.Eq(c => c.Id, complaintId);
            var update = Builders<Complaint>.Update.Set(c => c.Status, status);

            await _context.Complaints.UpdateOneAsync(filter, update);

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> AddSector(string state, string city, string sectorName)
        {
            var master = await _context.Masters.Find(_ => true).FirstOrDefaultAsync();
            if (master == null)
            {
                master = new Master();
                await _context.Masters.InsertOneAsync(master);
            }

            var filter = Builders<Master>.Filter.Eq(m => m.Id, master.Id);
            var update = Builders<Master>.Update.Push(m => m.Sectors, new SectorMapping { State = state, City = city, SectorName = sectorName });
            await _context.Masters.UpdateOneAsync(filter, update);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSector(string state, string city, string sectorName)
        {
            var master = await _context.Masters.Find(_ => true).FirstOrDefaultAsync();
            if (master != null)
            {
                var sector = master.Sectors.FirstOrDefault(s => s.State == state && s.City == city && s.SectorName == sectorName);
                if (sector != null)
                {
                    master.Sectors.Remove(sector);
                    var filter = Builders<Master>.Filter.Eq(m => m.Id, master.Id);
                    var update = Builders<Master>.Update.Set(m => m.Sectors, master.Sectors);
                    await _context.Masters.UpdateOneAsync(filter, update);
                }
            }
            return Json(new { success = true });
        }
        [HttpPost]
        public async Task<IActionResult> DeleteDepartment(string code)
        {
            var master = await _context.Masters.Find(_ => true).FirstOrDefaultAsync();
            if (master != null)
            {
                var dept = master.Departments.FirstOrDefault(d => d.Code == code);
                if (dept != null)
                {
                    master.Departments.Remove(dept);
                    var filter = Builders<Master>.Filter.Eq(m => m.Id, master.Id);
                    var update = Builders<Master>.Update.Set(m => m.Departments, master.Departments);
                    await _context.Masters.UpdateOneAsync(filter, update);
                }
            }
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            await _context.Users.DeleteOneAsync(filter);
            return Json(new { success = true });
        }
    }
}
