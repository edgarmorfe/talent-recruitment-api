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
    public class CompaniesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public CompaniesController(ApplicationDbContext db) => _db = db;

        // GET api/companies
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<CompanyDto>>> GetAll()
        {
            var companies = await _db.Companies
                .Select(c => Map(c))
                .ToListAsync();

            return Ok(companies);
        }

        // GET api/companies/5
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<CompanyDto>> GetById(int id)
        {
            var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == id);
            if (company == null) return NotFound();
            return Ok(Map(company));
        }

        // POST api/companies  (Admin only - recruiters get a company auto-created on registration)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<CompanyDto>> Create(CreateCompanyRequest request)
        {
            var company = new Company
            {
                Name = request.Name,
                Description = request.Description,
                Website = request.Website,
                Industry = request.Industry,
                LogoUrl = request.LogoUrl,
                Location = request.Location
            };

            _db.Companies.Add(company);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = company.Id }, Map(company));
        }

        // PUT api/companies/5  (Admin, or Recruiter belonging to that company)
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Recruiter")]
        public async Task<IActionResult> Update(int id, UpdateCompanyRequest request)
        {
            var company = await _db.Companies.FindAsync(id);
            if (company == null) return NotFound();

            if (!await UserCanManageCompanyAsync(id)) return Forbid();

            company.Name = request.Name;
            company.Description = request.Description;
            company.Website = request.Website;
            company.Industry = request.Industry;
            company.LogoUrl = request.LogoUrl;
            company.Location = request.Location;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // DELETE api/companies/5  (Admin only)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var company = await _db.Companies.FindAsync(id);
            if (company == null) return NotFound();

            _db.Companies.Remove(company);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        private async Task<bool> UserCanManageCompanyAsync(int companyId)
        {
            if (User.IsInRole("Admin")) return true;

            var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
            return await _db.Recruiters.AnyAsync(r => r.UserId == userId && r.CompanyId == companyId);
        }

        private static CompanyDto Map(Company c) => new()
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            Website = c.Website,
            Industry = c.Industry,
            LogoUrl = c.LogoUrl,
            Location = c.Location,
            JobPostCount = c.JobPosts?.Count ?? 0,
            RecruiterCount = c.Recruiters?.Count ?? 0
        };
    }
}
