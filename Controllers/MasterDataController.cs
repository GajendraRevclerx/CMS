using CMS.Services;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Threading.Tasks;

namespace CMS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MasterDataController : ControllerBase
    {
        private readonly MongoDbContext _context;

        public MasterDataController(MongoDbContext context)
        {
            _context = context;
        }

        [HttpGet("states")]
        public async Task<IActionResult> GetStates()
        {
            var master = await _context.Masters.Find(_ => true).FirstOrDefaultAsync();
            if (master == null) return NotFound();
            return Ok(master.States);
        }

        [HttpGet("cities")]
        public async Task<IActionResult> GetCities()
        {
            var master = await _context.Masters.Find(_ => true).FirstOrDefaultAsync();
            if (master == null) return NotFound();
            return Ok(master.Cities);
        }

        [HttpGet("departments")]
        public async Task<IActionResult> GetDepartments()
        {
            var departments = await _context.Departments.Find(_ => true).Project(d => new { d.Code, d.Name }).ToListAsync();
            return Ok(departments);
        }
        [HttpGet("next-sequence/{id}")]
        public async Task<IActionResult> GetNextSequence(string id)
        {
            var counter = await _context.Counters.Find(c => c.Id == id).FirstOrDefaultAsync();
            return Ok(new { currentValue = counter?.SequenceValue ?? 0 });
        }
    }
}
