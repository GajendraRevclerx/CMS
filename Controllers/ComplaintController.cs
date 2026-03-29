using CMS.Models;
using CMS.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Collections.Generic;
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

        public ComplaintController(MongoDbContext context, ComplaintService complaintService)
        {
            _context = context;
            _complaintService = complaintService;
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
        public async Task<IActionResult> Submit([FromBody] ComplaintSubmitRequest model)
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
                ComplaintTitle = model.ComplaintTitle,
                Description    = model.Description,
                Department     = model.Department,
                State          = model.State,
                City           = model.City,
                Street         = model.Street,
                Locality       = model.Locality,
                PinCode        = model.PinCode,
                Source         = model.Source,
                Site           = model.Site,
                IncidentDate   = model.IncidentDate
            };

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
