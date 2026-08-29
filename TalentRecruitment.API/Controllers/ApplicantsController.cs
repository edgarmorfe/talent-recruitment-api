using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TalentRecruitment.API.Data;
using TalentRecruitment.API.DTOs;
using TalentRecruitment.API.Models;

namespace TalentRecruitment.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ApplicantsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public ApplicantsController(ApplicationDbContext db) => _db = db;

        // GET api/applicants  (Admin/Recruiter can browse the talent pool)
        [HttpGet]
        [Authorize(Roles = "Admin,Recruiter")]
        public async Task<ActionResult<IEnumerable<ApplicantDto>>> GetAll([FromQuery] string? skill)
        {
            var query = _db.Applicants.Include(a => a.User).AsQueryable();

            if (!string.IsNullOrWhiteSpace(skill))
                query = query.Where(a => a.Skills != null && a.Skills.Contains(skill));

            var result = await query.Select(a => Map(a)).ToListAsync();
            return Ok(result);
        }

        // GET api/applicants/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ApplicantDto>> GetById(int id)
        {
            var applicant = await _db.Applicants.Include(a => a.User).FirstOrDefaultAsync(a => a.Id == id);
            if (applicant == null) return NotFound();

            if (!await CanViewApplicantAsync(applicant)) return Forbid();

            return Ok(Map(applicant));
        }

        // PUT api/applicants/5  (self only, or Admin)
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateApplicantRequest request)
        {
            var applicant = await _db.Applicants.FindAsync(id);
            if (applicant == null) return NotFound();

            var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
            if (!User.IsInRole("Admin") && applicant.UserId != userId) return Forbid();

            applicant.PhoneNumber = request.PhoneNumber;
            applicant.ResumeUrl = request.ResumeUrl;
            applicant.Skills = request.Skills;
            applicant.Education = request.Education;
            applicant.YearsOfExperience = request.YearsOfExperience;
            applicant.Location = request.Location;
            applicant.LinkedInUrl = request.LinkedInUrl;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        private Task<bool> CanViewApplicantAsync(Applicant applicant)
        {
            if (User.IsInRole("Admin") || User.IsInRole("Recruiter")) return Task.FromResult(true);

            var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
            return Task.FromResult(applicant.UserId == userId);
        }

        private static ApplicantDto Map(Applicant a) => new()
        {
            Id = a.Id,
            FullName = a.User.FullName,
            Email = a.User.Email,
            PhoneNumber = a.PhoneNumber,
            ResumeUrl = a.ResumeUrl,
            Skills = a.Skills,
            Education = a.Education,
            YearsOfExperience = a.YearsOfExperience,
            Location = a.Location,
            LinkedInUrl = a.LinkedInUrl
        };
    }
}
