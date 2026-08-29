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
    public class JobPostsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public JobPostsController(ApplicationDbContext db) => _db = db;

        // GET api/jobposts?status=Published&location=Manila&search=react
        // Public endpoint - applicants (and anonymous visitors) can browse open jobs.
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<JobPostDto>>> GetAll(
            [FromQuery] JobPostStatus? status,
            [FromQuery] string? location,
            [FromQuery] string? search,
            [FromQuery] int? companyId)
        {
            var query = _db.JobPosts
                .Include(j => j.Company)
                .Include(j => j.Recruiter).ThenInclude(r => r.User)
                .Include(j => j.JobApplications)
                .AsQueryable();

            // Anonymous / applicant callers only ever see Published posts.
            var isStaff = User.IsInRole("Admin") || User.IsInRole("Recruiter");
            if (!isStaff)
                query = query.Where(j => j.Status == JobPostStatus.Published);
            else if (status.HasValue)
                query = query.Where(j => j.Status == status.Value);

            if (!string.IsNullOrWhiteSpace(location))
                query = query.Where(j => j.Location != null && j.Location.Contains(location));

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(j => j.Title.Contains(search) || j.Description.Contains(search));

            if (companyId.HasValue)
                query = query.Where(j => j.CompanyId == companyId.Value);

            var result = await query.OrderByDescending(j => j.CreatedAtUtc).Select(j => Map(j)).ToListAsync();
            return Ok(result);
        }

        // GET api/jobposts/5
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<JobPostDto>> GetById(int id)
        {
            var jobPost = await _db.JobPosts
                .Include(j => j.Company)
                .Include(j => j.Recruiter).ThenInclude(r => r.User)
                .Include(j => j.JobApplications)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (jobPost == null) return NotFound();

            var isStaff = User.IsInRole("Admin") || User.IsInRole("Recruiter");
            if (jobPost.Status != JobPostStatus.Published && !isStaff) return NotFound();

            return Ok(Map(jobPost));
        }

        // POST api/jobposts  (Recruiter posts under their own company; Admin may target any
        // company by supplying CompanyId, optionally a specific RecruiterId within it)
        [HttpPost]
        [Authorize(Roles = "Admin,Recruiter")]
        public async Task<ActionResult<JobPostDto>> Create(CreateJobPostRequest request)
        {
            int companyId;
            int recruiterId;

            if (User.IsInRole("Admin"))
            {
                if (!request.CompanyId.HasValue)
                    return BadRequest(new { message = "CompanyId is required when creating a job post as Admin." });

                var company = await _db.Companies.FindAsync(request.CompanyId.Value);
                if (company == null) return BadRequest(new { message = "Company not found." });
                companyId = company.Id;

                if (request.RecruiterId.HasValue)
                {
                    var chosenRecruiter = await _db.Recruiters.FindAsync(request.RecruiterId.Value);
                    if (chosenRecruiter == null || chosenRecruiter.CompanyId != companyId)
                        return BadRequest(new { message = "RecruiterId does not belong to the given company." });
                    recruiterId = chosenRecruiter.Id;
                }
                else
                {
                    var anyRecruiter = await _db.Recruiters.FirstOrDefaultAsync(r => r.CompanyId == companyId);
                    if (anyRecruiter == null)
                        return BadRequest(new { message = "This company has no recruiter to assign the post to. Create a recruiter for it first." });
                    recruiterId = anyRecruiter.Id;
                }
            }
            else
            {
                var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
                var recruiter = await _db.Recruiters.FirstOrDefaultAsync(r => r.UserId == userId);
                if (recruiter == null) return Forbid();

                companyId = recruiter.CompanyId;
                recruiterId = recruiter.Id;
            }

            var jobPost = new JobPost
            {
                Title = request.Title,
                Description = request.Description,
                Requirements = request.Requirements,
                Location = request.Location,
                IsRemote = request.IsRemote,
                EmploymentType = request.EmploymentType,
                SalaryMin = request.SalaryMin,
                SalaryMax = request.SalaryMax,
                ClosingDateUtc = request.ClosingDateUtc,
                CompanyId = companyId,
                RecruiterId = recruiterId,
                Status = JobPostStatus.Draft
            };

            _db.JobPosts.Add(jobPost);
            await _db.SaveChangesAsync();

            await _db.Entry(jobPost).Reference(j => j.Company).LoadAsync();
            await _db.Entry(jobPost).Reference(j => j.Recruiter).LoadAsync();
            await _db.Entry(jobPost.Recruiter).Reference(r => r.User).LoadAsync();

            return CreatedAtAction(nameof(GetById), new { id = jobPost.Id }, Map(jobPost));
        }

        // PUT api/jobposts/5  (owning Recruiter or Admin)
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Recruiter")]
        public async Task<IActionResult> Update(int id, UpdateJobPostRequest request)
        {
            var jobPost = await _db.JobPosts.FindAsync(id);
            if (jobPost == null) return NotFound();
            if (!await UserOwnsJobPostAsync(jobPost)) return Forbid();

            jobPost.Title = request.Title;
            jobPost.Description = request.Description;
            jobPost.Requirements = request.Requirements;
            jobPost.Location = request.Location;
            jobPost.IsRemote = request.IsRemote;
            jobPost.EmploymentType = request.EmploymentType;
            jobPost.SalaryMin = request.SalaryMin;
            jobPost.SalaryMax = request.SalaryMax;
            jobPost.ClosingDateUtc = request.ClosingDateUtc;

            if (jobPost.Status != JobPostStatus.Published && request.Status == JobPostStatus.Published)
                jobPost.PublishedAtUtc = DateTime.UtcNow;

            jobPost.Status = request.Status;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // DELETE api/jobposts/5  (owning Recruiter or Admin)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Recruiter")]
        public async Task<IActionResult> Delete(int id)
        {
            var jobPost = await _db.JobPosts.FindAsync(id);
            if (jobPost == null) return NotFound();
            if (!await UserOwnsJobPostAsync(jobPost)) return Forbid();

            _db.JobPosts.Remove(jobPost);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        private async Task<bool> UserOwnsJobPostAsync(JobPost jobPost)
        {
            if (User.IsInRole("Admin")) return true;

            var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
            var recruiter = await _db.Recruiters.FirstOrDefaultAsync(r => r.UserId == userId);
            return recruiter != null && recruiter.Id == jobPost.RecruiterId;
        }

        private static JobPostDto Map(JobPost j) => new()
        {
            Id = j.Id,
            Title = j.Title,
            Description = j.Description,
            Requirements = j.Requirements,
            Location = j.Location,
            IsRemote = j.IsRemote,
            EmploymentType = j.EmploymentType,
            SalaryMin = j.SalaryMin,
            SalaryMax = j.SalaryMax,
            Status = j.Status,
            CompanyId = j.CompanyId,
            CompanyName = j.Company?.Name ?? string.Empty,
            RecruiterId = j.RecruiterId,
            RecruiterName = j.Recruiter?.User?.FullName ?? string.Empty,
            CreatedAtUtc = j.CreatedAtUtc,
            PublishedAtUtc = j.PublishedAtUtc,
            ClosingDateUtc = j.ClosingDateUtc,
            ApplicationCount = j.JobApplications?.Count ?? 0
        };
    }
}
