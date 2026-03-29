using CMS.Services;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Linq;
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
            var master = await _context.Masters.Find(_ => true).FirstOrDefaultAsync();
            if (master == null) return NotFound();
            return Ok(master.Departments);
        }

        [HttpGet("sectors-by-city")]
        public async Task<IActionResult> GetSectorsByCity(string city)
        {
            var master = await _context.Masters.Find(_ => true).FirstOrDefaultAsync();
            if (master == null) return NotFound();
            var sectors = master.Sectors.Where(s => s.City == city).Select(s => s.SectorName).ToList();
            return Ok(sectors);
        }

        [HttpGet("cities-by-state")]
        public async Task<IActionResult> GetCitiesByState(string state)
        {
            var master = await _context.Masters.Find(_ => true).FirstOrDefaultAsync();
            if (master == null) return NotFound();
            var cities = master.Sectors.Where(s => s.State == state).Select(s => s.City).Distinct().ToList();
            return Ok(cities);
        }

        [HttpGet("areas")]
        public async Task<IActionResult> GetAreas()
        {
            var master = await _context.Masters.Find(_ => true).FirstOrDefaultAsync();
            if (master == null) return NotFound();
            return Ok(master.Areas);
        }

        [HttpGet("find-head")]
        public async Task<IActionResult> FindHead(string dept, string area)
        {
            var head = await _context.Users.Find(u => u.Role == "DeptHead" && u.Department == dept && u.Area == area).FirstOrDefaultAsync();
            if (head == null) return NotFound(new { message = "No head assigned yet" });
            return Ok(new { id = head.Id, name = head.FullName });
        }
    }
}
