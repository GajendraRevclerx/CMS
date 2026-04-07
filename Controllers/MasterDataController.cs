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
            if (string.Equals(city, "Chandigarh", System.StringComparison.OrdinalIgnoreCase))
            {
                return Ok(new[] {
                    "Sector 1", "Sector 2", "Sector 3", "Sector 4", "Sector 5", "Sector 6", "Sector 7", "Sector 8", "Sector 9", "Sector 10",
                    "Sector 11", "Sector 12", "Sector 13 (Manimajra)", "Sector 14", "Sector 15", "Sector 16", "Sector 17", "Sector 18", "Sector 19", "Sector 20",
                    "Sector 21", "Sector 22", "Sector 23", "Sector 24", "Sector 25", "Sector 26", "Sector 27", "Sector 28", "Sector 29", "Sector 30",
                    "Sector 31", "Sector 32", "Sector 33", "Sector 34", "Sector 35", "Sector 36", "Sector 37", "Sector 38", "Sector 38 West", "Sector 39", "Sector 40",
                    "Sector 41", "Sector 42", "Sector 43", "Sector 44", "Sector 45", "Sector 46", "Sector 47", "Sector 48", "Sector 49", "Sector 50",
                    "Sector 51", "Sector 52", "Sector 53", "Sector 54", "Sector 55", "Sector 56", "Sector 61", "Sector 63",
                    "Industrial Area Phase 1", "Industrial Area Phase 2",
                    "Manimajra", "Burail", "Kajheri", "Palsora", "Maloya", "Dadumajra", "Dhanas", "Khuda Lahora", "Khuda Jassu", "Khuda Ali Sher",
                    "Kishangarh", "Kaimbwala", "Sarangpur", "Behlana", "Raipur Khurd", "Raipur Kalan", "Makhan Majra", "Daria", "Mauli Jagran", "Vikas Nagar"
                });
            }

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

        [HttpGet("divisions-by-department")]
        public async Task<IActionResult> GetDivisionsByDepartment(string department)
        {
            var master = await _context.Masters.Find(_ => true).FirstOrDefaultAsync();
            if (master == null) return NotFound();
            var divisions = master.Divisions.Where(d => d.DepartmentName == department).ToList();
            return Ok(divisions);
        }

        [HttpGet("sub-divisions-by-division")]
        public async Task<IActionResult> GetSubDivisionsByDivision(string division)
        {
            var master = await _context.Masters.Find(_ => true).FirstOrDefaultAsync();
            if (master == null) return NotFound();
            var subDivs = master.SubDivisions.Where(s => s.DivisionName == division).ToList();
            return Ok(subDivs);
        }

        [HttpGet("find-head")]
        public async Task<IActionResult> FindHead(string dept, string area)
        {
            var head = await _context.Users.Find(u => u.Role == "DeptHead" && u.Department.Contains(dept) && u.Area == area).FirstOrDefaultAsync();
            if (head == null) return NotFound(new { message = "No head assigned yet" });
            return Ok(new { id = head.Id, name = head.FullName });
        }
        [HttpGet("officers")]
        public async Task<IActionResult> GetOfficers()
        {
            var officers = await _context.Users.Find(u => u.Role == "DeptHead").ToListAsync();
            return Ok(officers.Select(o => new { id = o.Id, fullName = o.FullName, department = o.Department }));
        }
    }
}
