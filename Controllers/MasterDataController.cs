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
            var master = await _context.Masters.Find(_ => true).FirstOrDefaultAsync();
            if (master == null) return NotFound();
            return Ok(master.Departments);
        }
    }
}
