using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TalentRecruitment.API.Data;
using TalentRecruitment.API.DTOs;

namespace TalentRecruitment.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RecruitersController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public RecruitersController(ApplicationDbContext db) => _db = db;

        // GET api/recruiters?companyId=1
        [HttpGet]
        [Authorize(Roles = "Admin,Recruiter")]
        public async Task<ActionResult<IEnumerable<RecruiterDto>>> GetAll([FromQuery] int? companyId)
        {
            var query = _db.Recruiters.Include(r => r.User).Include(r => r.Company).AsQueryable();

            if (companyId.HasValue)
                query = query.Where(r => r.CompanyId == companyId.Value);

            var result = await query.Select(r => MapStatic(r)).ToListAsync();
            return Ok(result);
        }

        // GET api/recruiters/5
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Recruiter")]
        public async Task<ActionResult<RecruiterDto>> GetById(int id)
        {
            var recruiter = await _db.Recruiters.Include(r => r.User).Include(r => r.Company)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (recruiter == null) return NotFound();
            return Ok(MapStatic(recruiter));
        }

        // PUT api/recruiters/5  (self, or Admin)
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Recruiter")]
        public async Task<IActionResult> Update(int id, UpdateRecruiterRequest request)
        {
            var recruiter = await _db.Recruiters.FindAsync(id);
            if (recruiter == null) return NotFound();

            var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
            if (!User.IsInRole("Admin") && recruiter.UserId != userId) return Forbid();

            recruiter.JobTitle = request.JobTitle;
            recruiter.PhoneNumber = request.PhoneNumber;
            await _db.SaveChangesAsync();

            return NoContent();
        }

        private static RecruiterDto MapStatic(Models.Recruiter r) => new()
        {
            Id = r.Id,
            FullName = r.User.FullName,
            Email = r.User.Email,
            JobTitle = r.JobTitle,
            PhoneNumber = r.PhoneNumber,
            CompanyId = r.CompanyId,
            CompanyName = r.Company.Name
        };
    }
}
