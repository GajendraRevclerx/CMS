using CMS.Models;
using CMS.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace CMS.Controllers
{
    [Authorize]
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
        public IActionResult Submit()
        {
            return View();
        }

        /// <summary>
        /// Combined Register + Submit Complaint in one step.
        /// If the citizen is already logged in, registration fields are ignored.
        /// If anonymous: creates account (or verifies existing) then signs in.
        /// </summary>
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Submit([FromForm] ComplaintSubmitRequest model)
        {
            if (model == null)
                return BadRequest(new { success = false, message = "Invalid request." });

            string userMobile;

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                // Already logged in — use their mobile from claims
                userMobile = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            }
            else
            {
                // Anonymous: register or verify existing account
                if (string.IsNullOrWhiteSpace(model.FullName) ||
                    string.IsNullOrWhiteSpace(model.MobileNo) ||
                    string.IsNullOrWhiteSpace(model.Password))
                {
                    return BadRequest(new { success = false, message = "Full Name, Mobile Number, and Password are required to register." });
                }

                if (model.MobileNo.Length != 10)
                    return BadRequest(new { success = false, message = "Mobile number must be 10 digits." });

                var existingUser = await _context.Users
                    .Find(u => u.MobileNo == model.MobileNo)
                    .FirstOrDefaultAsync();

                if (existingUser != null)
                {
                    // User already exists — verify password
                    if (existingUser.Password != model.Password)
                        return BadRequest(new { success = false, message = "An account with this mobile number already exists. Please enter the correct password to submit your complaint." });
                }
                else
                {
                    // Create new citizen account
                    var newUser = new User
                    {
                        FullName  = model.FullName,
                        MobileNo  = model.MobileNo,
                        Password  = model.Password,
                        Email     = model.Email ?? "",
                        Role      = "Citizen"
                    };
                    await _context.Users.InsertOneAsync(newUser);
                }

                userMobile = model.MobileNo;

                // Sign the citizen in
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userMobile),
                    new Claim(ClaimTypes.Name, model.FullName),
                    new Claim(ClaimTypes.Role, "Citizen")
                };
                var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(identity));
            }

            // Build the complaint model
            var complaint = new Complaint
            {
                UserId         = userMobile,
                ComplaintTitle = model.ComplaintTitle ?? string.Empty,
                Description    = model.Description ?? string.Empty,
                Department     = model.Department ?? string.Empty,
                State          = model.State ?? string.Empty,
                City           = model.City ?? string.Empty,
                Street         = model.Street ?? string.Empty,
                Locality       = model.Locality ?? string.Empty,
                PinCode        = model.PinCode ?? string.Empty,
                Source         = model.Source ?? string.Empty,
                Site           = model.Site ?? string.Empty,
                IncidentDate   = model.IncidentDate ?? string.Empty
            };

            // Handle Evidence Upload
            if (model.EvidenceFile != null && model.EvidenceFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "evidence");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{Guid.NewGuid()}_{model.EvidenceFile.FileName}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.EvidenceFile.CopyToAsync(fileStream);
                }
                complaint.EvidencePath = $"/uploads/evidence/{uniqueFileName}";
            }

            if (complaint.Description == null || complaint.Description.Length < 10)
                return BadRequest(new { success = false, message = "Description must be at least 10 characters." });

            await _complaintService.CreateComplaintAsync(complaint);

            return Ok(new { success = true, complaintNo = complaint.ComplaintNo });
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var userMobile = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var complaints = await _context.Complaints
                .Find(c => c.UserId == userMobile)
                .SortByDescending(c => c.CreatedDate)
                .ToListAsync();

            return View(complaints);
        }
    }
}
