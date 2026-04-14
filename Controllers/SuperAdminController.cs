using CMS.Models;
using CMS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace CMS.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class SuperAdminController : Controller
    {
        private readonly MongoDbContext _context;

        public SuperAdminController(MongoDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users.Find(u => u.Role == "Admin" || u.Role == "Helpdesk").ToListAsync();
            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> UpsertAdmin([FromForm] User model)
        {
            if (string.IsNullOrEmpty(model.FullName) || string.IsNullOrEmpty(model.MobileNo))
            {
                return Json(new { success = false, message = "Full Name and Mobile No. are required." });
            }

            // Fallback to Admin if no role provided
            if (string.IsNullOrEmpty(model.Role)) model.Role = "Admin"; 

            if (string.IsNullOrEmpty(model.Id))
            {
                // Create New
                var existing = await _context.Users.Find(u => u.MobileNo == model.MobileNo).FirstOrDefaultAsync();
                if (existing != null)
                {
                    return Json(new { success = false, message = "Mobile Number already registered for another user." });
                }

                if (string.IsNullOrEmpty(model.Password))
                {
                    model.Password = model.MobileNo; 
                }

                await _context.Users.InsertOneAsync(model);
                return Json(new { success = true, message = $"{model.Role} created successfully! Default password is the mobile number." });
            }
            else
            {
                // Update Existing
                var filter = Builders<User>.Filter.Eq(u => u.Id, model.Id);
                var update = Builders<User>.Update
                    .Set(u => u.FullName, model.FullName)
                    .Set(u => u.MobileNo, model.MobileNo)
                    .Set(u => u.Email, model.Email)
                    .Set(u => u.Role, model.Role);

                if (!string.IsNullOrEmpty(model.Password))
                {
                    update = update.Set(u => u.Password, model.Password);
                }

                await _context.Users.UpdateOneAsync(filter, update);
                return Json(new { success = true, message = "User details updated successfully!" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAdmin(string id)
        {
            if (string.IsNullOrEmpty(id)) return Json(new { success = false, message = "ID is required." });
            
            var result = await _context.Users.DeleteOneAsync(u => u.Id == id && (u.Role == "Admin" || u.Role == "Helpdesk"));
            if (result.DeletedCount > 0)
            {
                return Json(new { success = true, message = "User removed successfully from registry." });
            }
            return Json(new { success = false, message = "Could not find user to delete." });
        }
    }
}
