using CMS.Models;
using CMS.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
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

        [Authorize(Roles = "Helpdesk")]
        [HttpGet]
        public async Task<IActionResult> DirectSubmit()
        {
            var userMobile = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            var isStaff = User.IsInRole("Helpdesk") || User.IsInRole("DeptHead") || userRole == "Helpdesk" || userRole == "DeptHead";

            var complaints = await _context.Complaints
                .Find(c => isStaff ? true : c.UserId == userMobile)
                .ToListAsync();

            ViewBag.Total = complaints.Count;
            ViewBag.UnassignedCount = await _context.Complaints.CountDocumentsAsync(c => string.IsNullOrEmpty(c.AssignedToId));
            
            var now = DateTime.UtcNow;
            ViewBag.Escalated = complaints.Count(c => c.Status != "Resolved" && c.Status != "Closed" && (now - c.CreatedDate).TotalDays > 1);

            return View();
        }

        [Authorize(Roles = "Helpdesk")]
        [HttpPost]
        public async Task<IActionResult> DirectSubmit([FromForm] DirectComplaintViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Chandigarh-specific server-side validation
                if (model.State != "Chandigarh" || model.City != "Chandigarh")
                    return BadRequest(new { success = false, message = "Only Chandigarh is supported for registration." });
                
                if (!string.IsNullOrEmpty(model.PinCode) && !model.PinCode.StartsWith("160"))
                    return BadRequest(new { success = false, message = "Invalid Chandigarh Pin Code. Area must be within 160xxx series." });

                var allowedSources = new[] { "Mobile", "WhatsApp" };
                if (string.IsNullOrEmpty(model.Source) || !allowedSources.Contains(model.Source))
                    return BadRequest(new { success = false, message = "Invalid registration source." });

                // Note: Stop Automatic User Creation as requested.
                // We just use the provided contact info for the complaint.

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

                var staffMobile = User.FindFirstValue(ClaimTypes.NameIdentifier);
                // 3. Create Complaint
                var complaint = new Complaint
                {
                    ComplaintNo = model.ComplaintNo ?? $"CHD/{DateTime.Now.Year}/{Guid.NewGuid().ToString().Substring(0, 5).ToUpper()}",
                    UserId = model.MobileNo,
                    RegisteredById = staffMobile,
                    FullName = model.FullName,
                    Email = model.Email,
                    ComplaintTitle = model.ComplaintTitle,
                    Description = model.Description,
                    State = model.State,
                    City = model.City,
                    Area = model.Area ?? model.AreaOfJurisdiction,
                    AreaOfJurisdiction = model.AreaOfJurisdiction ?? model.Area,
                    Department = model.Department,
                    Street = model.Street,
                    Locality = model.Locality,
                    PinCode = model.PinCode,
                    Site = model.Site,
                    Source = model.Source,
                    IncidentDate = model.IncidentDate,
                    AssignedToId = model.AssignedToId,
                    AssignedToName = model.AssignedToName,
                    AssignedToMobile = !string.IsNullOrEmpty(model.AssignedToId) ? (await _context.Users.Find(u => u.Id == model.AssignedToId).FirstOrDefaultAsync())?.MobileNo : null,
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
                // Chandigarh-specific server-side validation
                if (model.State != "Chandigarh" || model.City != "Chandigarh")
                    return BadRequest(new { success = false, message = "Only Chandigarh is supported for registration." });
                
                if (!string.IsNullOrEmpty(model.PinCode) && !model.PinCode.StartsWith("160"))
                    return BadRequest(new { success = false, message = "Invalid Chandigarh Pin Code. Area must be within 160xxx series." });

                var allowedSources = new[] { "Mobile", "WhatsApp" };
                if (string.IsNullOrEmpty(model.Source) || !allowedSources.Contains(model.Source))
                    return BadRequest(new { success = false, message = "Invalid registration source." });

                var userMobile = User.FindFirstValue(ClaimTypes.NameIdentifier);
                model.UserId = userMobile ?? "";

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
        public async Task<IActionResult> GetNextId()
        {
            var nextId = await _complaintService.GetNextComplaintIdAsync();
            return Ok(new { nextId });
        }

        [Authorize(Roles = "Helpdesk,Citizen")]
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var userMobile = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            var isStaff = User.IsInRole("Helpdesk") || User.IsInRole("DeptHead") || userRole == "Helpdesk" || userRole == "DeptHead";

            var complaints = await _context.Complaints
                .Find(c => isStaff ? true : c.UserId == userMobile) // Staff sees all, Citizen sees self
                .SortByDescending(c => c.CreatedDate)
                .ToListAsync();

            if (isStaff)
            {
                var allPending = await _context.Complaints
                    .Find(c => string.IsNullOrEmpty(c.AssignedToId))
                    .SortByDescending(c => c.CreatedDate)
                    .ToListAsync();
                ViewBag.AllPending = allPending;
                ViewBag.UnassignedCount = allPending.Count;
            }

            var now = DateTime.UtcNow;
            ViewBag.Total = complaints.Count;
            ViewBag.Pending = complaints.Count(c => c.Status == "Pending");
            ViewBag.Assigned = complaints.Count(c => c.Status == "Assigned");
            ViewBag.Resolved = complaints.Count(c => c.Status == "Resolved");
            ViewBag.Escalated = complaints.Count(c => c.Status != "Resolved" && c.Status != "Closed" && (now - c.CreatedDate).TotalDays > 1);

            var resolvedComplaints = complaints.Where(c => c.Status == "Resolved" && c.ResolutionDate != null).ToList();
            double avgTime = resolvedComplaints.Any() ? resolvedComplaints.Average(c => (c.ResolutionDate!.Value - c.CreatedDate).TotalDays) : 0;
            ViewBag.AvgResolutionTime = avgTime.ToString("F1") + "d";

            var ratedComplaints = complaints.Where(c => c.Rating > 0).ToList();
            double avgRating = ratedComplaints.Any() ? ratedComplaints.Average(c => c.Rating) : 0;
            ViewBag.AvgRating = avgRating.ToString("F1") + "/5";

            if (User.IsInRole("SuperAdmin") || userRole == "SuperAdmin") return RedirectToAction("Index", "SuperAdmin");
            if (User.IsInRole("Admin") || userRole == "Admin") return RedirectToAction("Index", "Admin");

            if (isStaff)
            {
                ViewBag.Users = await _context.Users.Find(_ => true).ToListAsync();
                ViewBag.Heads = await _context.Users.Find(u => u.Role == "DeptHead").ToListAsync();
                var masters = await _context.Masters.Find(_ => true).FirstOrDefaultAsync() ?? new Master();
                ViewBag.Masters = masters;
                ViewBag.AllDepartments = masters.Departments.Select(d => new CMS.Models.DepartmentBrief { Name = d.Name, Code = d.Code }).ToList();
                return View("HelpdeskDashboard", complaints);
            }

            return RedirectToAction("Index", "Home");
        }

        [Authorize(Roles = "Helpdesk,Admin")]
        [HttpPost]
        public async Task<IActionResult> Reassign(string complaintId, string headId, string headName)
        {
            var filter = Builders<Complaint>.Filter.Eq(c => c.Id, complaintId);
            var officer = await _context.Users.Find(u => u.Id == headId).FirstOrDefaultAsync();
            var update = Builders<Complaint>.Update
                .Set(c => c.AssignedToId, headId)
                .Set(c => c.AssignedToName, headName)
                .Set(c => c.AssignedToMobile, officer?.MobileNo)
                .Set(c => c.Status, "Assigned");

            await _context.Complaints.UpdateOneAsync(filter, update);
            return Json(new { success = true });
        }

        [Authorize(Roles = "Helpdesk,Admin")]
        [HttpPost]
        public async Task<IActionResult> ResetAssignment(string complaintId)
        {
            var filter = Builders<Complaint>.Filter.Eq(c => c.Id, complaintId);
            var update = Builders<Complaint>.Update
                .Set(c => c.AssignedToId, null)
                .Set(c => c.AssignedToName, null)
                .Set(c => c.AssignedToMobile, null)
                .Set(c => c.Division, null)
                .Set(c => c.SubDivision, null)
                .Set(c => c.Status, "Pending");

            await _context.Complaints.UpdateOneAsync(filter, update);
            return Json(new { success = true });
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Track(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest(new { success = false, message = "Complaint number is required." });

            var complaint = await _context.Complaints
                .Find(c => c.ComplaintNo == id)
                .FirstOrDefaultAsync();

            if (complaint == null) return NotFound(new { success = false, message = "No complaint found with this number." });

            string timeTaken = "—";
            if (complaint.ResolutionDate.HasValue)
            {
                var diff = complaint.ResolutionDate.Value - complaint.CreatedDate;
                timeTaken = $"{(int)diff.TotalDays}d {diff.Hours}h {diff.Minutes}m";
            }

            var masters = await _context.Masters.Find(_ => true).FirstOrDefaultAsync() ?? new Master();
            var deptName = masters.Departments.FirstOrDefault(d => d.Code == complaint.Department || d.Name == complaint.Department)?.Name ?? complaint.Department;

            string officerDeptName = "—";
            string officerDivision = "—";
            string officerSubDivision = "—";

            if (!string.IsNullOrEmpty(complaint.AssignedToId))
            {
                var officer = await _context.Users.Find(u => u.Id == complaint.AssignedToId).FirstOrDefaultAsync();
                if (officer != null)
                {
                    if (!string.IsNullOrEmpty(officer.Department))
                        officerDeptName = masters.Departments.FirstOrDefault(d => d.Code == officer.Department || d.Name == officer.Department)?.Name ?? officer.Department;
                    
                    officerDivision = officer.Division ?? "—";
                    officerSubDivision = officer.SubDivision ?? "—";
                }
            }

            return Ok(new {
                success = true,
                complaintNo = complaint.ComplaintNo,
                userId = complaint.UserId,
                status = complaint.Status,
                department = deptName,
                officerDept = officerDeptName,
                officerDivision = officerDivision,
                officerSubDivision = officerSubDivision,
                createdDate = complaint.CreatedDate.ToString("dd/MM/yyyy HH:mm:ss"),
                fullName = complaint.FullName ?? "—",
                email = complaint.Email ?? "—",
                address = $"{complaint.Locality}, {complaint.PinCode}",
                street = complaint.Street ?? "—",
                locality = complaint.Locality ?? "—",
                pinCode = complaint.PinCode ?? "—",
                area = complaint.Area ?? "—",
                site = complaint.Site ?? "—",
                source = complaint.Source ?? "—",
                incidentDate = complaint.IncidentDate != null ? complaint.IncidentDate.Value.ToString("dd/MM/yyyy") : "—",
                title = complaint.ComplaintTitle,
                description = complaint.Description ?? "—",
                priority = complaint.Priority ?? "Medium",
                assignedTo = complaint.AssignedToName ?? "Pending",
                assignedToMobile = complaint.AssignedToMobile ?? "—",
                division = complaint.Division ?? "—",
                subDivision = complaint.SubDivision ?? "—",
                resolutionDate = complaint.ResolutionDate?.ToString("dd/MM/yyyy HH:mm:ss") ?? "—",
                resolutionRemark = complaint.ResolutionRemark ?? "—",
                timeTaken = timeTaken
            });
        }

        [Authorize(Roles = "Helpdesk,Admin")]
        [HttpGet]
        public async Task<IActionResult> GetComplaintByNo(string complaintNo)
        {
            var complaint = await _context.Complaints.Find(c => c.ComplaintNo == complaintNo).FirstOrDefaultAsync();
            if (complaint == null) return NotFound(new { success = false, message = "Complaint not found." });
            return Ok(new { success = true, complaint });
        }

        [Authorize(Roles = "Helpdesk,Admin")]
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(string complaintId, string status, string? remark)
        {
            var filter = Builders<Complaint>.Filter.Eq(c => c.Id, complaintId);
            var update = Builders<Complaint>.Update.Set(c => c.Status, status);
            
            if (status == "Resolved") {
                update = update.Set(c => c.ResolutionDate, System.DateTime.UtcNow)
                               .Set(c => c.ResolutionRemark, remark);
            }

            await _context.Complaints.UpdateOneAsync(filter, update);
            return Json(new { success = true });
        }

        [Authorize(Roles = "Helpdesk,Admin")]
        [HttpPost]
        public async Task<IActionResult> UpdatePriority(string complaintId, string priority)
        {
            var filter = Builders<Complaint>.Filter.Eq(c => c.Id, complaintId);
            var update = Builders<Complaint>.Update.Set(c => c.Priority, priority);

            await _context.Complaints.UpdateOneAsync(filter, update);
            return Json(new { success = true });
        }
    }
}
