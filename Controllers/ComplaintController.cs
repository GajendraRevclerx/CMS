using CMS.Models;
using CMS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;
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

        [HttpGet]
        public IActionResult Submit()
        {
            return View();
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

            return View(complaints);
        }
    }
}
