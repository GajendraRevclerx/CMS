using CMS.Models;
using CMS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CMS.Controllers
{
    public class ComplaintController : Controller
    {
        private readonly MongoDbContext _context;
        private readonly ComplaintService _complaintService;
        private readonly IWebHostEnvironment _environment;

        public ComplaintController(MongoDbContext context, ComplaintService complaintService, IWebHostEnvironment environment)
        {
            _context = context;
            _complaintService = complaintService;
            _environment = environment;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult DirectSubmit()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> DirectSubmit([FromForm] DirectComplaintViewModel model)
        {
            if (ModelState.IsValid)
            {
                // 1. Check if user exists
                var user = await _context.Users.Find(u => u.MobileNo == model.MobileNo).FirstOrDefaultAsync();
                
                if (user == null)
                {
                    // Create new user
                    user = new User
                    {
                        FullName = model.FullName,
                        MobileNo = model.MobileNo,
                        Email = model.Email,
                        Password = model.Password ?? model.MobileNo // Use mobile as default if not provided
                    };
                    await _context.Users.InsertOneAsync(user);
                }
                else
                {
                    // Existing user - update password if provided
                    if (!string.IsNullOrEmpty(model.Password))
                    {
                        var update = Builders<User>.Update.Set(u => u.Password, model.Password);
                        await _context.Users.UpdateOneAsync(u => u.MobileNo == model.MobileNo, update);
                    }
                }

                // 2. Handle File Attachment
                string? attachmentPath = null;
                if (model.Attachment != null && model.Attachment.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + model.Attachment.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.Attachment.CopyToAsync(fileStream);
                    }
                    attachmentPath = "/uploads/" + uniqueFileName;
                }

                // 3. Create Complaint
                var complaint = new Complaint
                {
                    ComplaintNo = model.ComplaintNo ?? $"CHD/{DateTime.Now.Year}/{Guid.NewGuid().ToString().Substring(0, 5).ToUpper()}",
                    UserId = user.MobileNo,
                    ComplaintTitle = model.ComplaintTitle,
                    Description = model.Description,
                    State = model.State,
                    City = model.City,
                    Area = model.Area,
                    Department = model.Department,
                    Street = model.Street,
                    Locality = model.Locality,
                    PinCode = model.PinCode,
                    Site = model.Site,
                    Source = model.Source,
                    IncidentDate = model.IncidentDate,
                    AssignedToId = model.AssignedToId,
                    AssignedToName = model.AssignedToName,
                    AttachmentPath = attachmentPath,
                    CreatedDate = DateTime.UtcNow,
                    Status = string.IsNullOrEmpty(model.AssignedToId) ? "Pending" : "Assigned"
                };

                await _complaintService.CreateComplaintAsync(complaint);
                
                return Ok(new { success = true, complaintNo = complaint.ComplaintNo, message = "Successfully submitted with attachment! You can track this by logging in." });
            }
            return BadRequest(new { success = false, message = "Invalid data provided." });
        }

        [Authorize]
        [HttpGet]
        public IActionResult Submit()
        {
            return RedirectToAction("DirectSubmit");
        }

        [HttpPost]
        public async Task<IActionResult> Submit([FromBody] Complaint model)
        {
            if (ModelState.IsValid)
            {
                var userMobile = User.FindFirstValue(ClaimTypes.NameIdentifier);
                model.UserId = userMobile ?? "";

                await _complaintService.CreateComplaintAsync(model);
                
                return Ok(new { success = true, complaintNo = model.ComplaintNo });
            }
            return BadRequest(new { success = false, message = "Invalid data" });
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var userMobile = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var complaints = await _context.Complaints
                .Find(c => c.UserId == userMobile)
                .SortByDescending(c => c.CreatedDate)
                .ToListAsync();

            ViewBag.Total = complaints.Count;
            ViewBag.Pending = complaints.Count(c => c.Status == "Pending");
            ViewBag.InProgress = complaints.Count(c => c.Status == "In Progress" || c.Status == "Assigned");
            ViewBag.Resolved = complaints.Count(c => c.Status == "Resolved");

            return View(complaints);
        }
    }
}
