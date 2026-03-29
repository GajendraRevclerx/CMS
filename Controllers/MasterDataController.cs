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
            var states = await _context.States.Find(_ => true)
                .Project(s => new { s.Name, s.ShortCode })
                .ToListAsync();
            return Ok(states);
        }

        [HttpGet("cities/{stateCode}")]
        public async Task<IActionResult> GetCities(string stateCode)
        {
            var cities = await _context.Cities.Find(c => c.StateCode == stateCode)
                .Project(c => c.Name)
                .ToListAsync();
            return Ok(cities);
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
