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
    public class JobApplicationsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public JobApplicationsController(ApplicationDbContext db) => _db = db;

        // GET api/jobapplications/mine  (Applicant: their own submitted applications)
        [HttpGet("mine")]
        [Authorize(Roles = "Applicant")]
        public async Task<ActionResult<IEnumerable<JobApplicationDto>>> GetMyApplications()
        {
            var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
            var applicant = await _db.Applicants.FirstOrDefaultAsync(a => a.UserId == userId);
            if (applicant == null) return Ok(Array.Empty<JobApplicationDto>());

            var result = await _db.JobApplications
                .Include(ja => ja.JobPost).ThenInclude(jp => jp.Company)
                .Include(ja => ja.Applicant).ThenInclude(a => a.User)
                .Where(ja => ja.ApplicantId == applicant.Id)
                .OrderByDescending(ja => ja.AppliedAtUtc)
                .Select(ja => Map(ja))
                .ToListAsync();

            return Ok(result);
        }

        // GET api/jobapplications/by-jobpost/5  (Recruiter/Admin: applicants for a given posting)
        [HttpGet("by-jobpost/{jobPostId}")]
        [Authorize(Roles = "Admin,Recruiter")]
        public async Task<ActionResult<IEnumerable<JobApplicationDto>>> GetByJobPost(int jobPostId)
        {
            var jobPost = await _db.JobPosts.FindAsync(jobPostId);
            if (jobPost == null) return NotFound();
            if (!await UserOwnsJobPostAsync(jobPost)) return Forbid();

            var result = await _db.JobApplications
                .Include(ja => ja.JobPost).ThenInclude(jp => jp.Company)
                .Include(ja => ja.Applicant).ThenInclude(a => a.User)
                .Where(ja => ja.JobPostId == jobPostId)
                .OrderByDescending(ja => ja.AppliedAtUtc)
                .Select(ja => Map(ja))
                .ToListAsync();

            return Ok(result);
        }

        // GET api/jobapplications/dashboard?search=react&status=Shortlisted&endorsed=true
        // Recruiter Dashboard data source: every application across every job post the caller
        // manages (or, for Admins, across the whole platform), with a single free-text "smart
        // search" box that matches job title, company, applicant name/email, status, or remarks.
        // The frontend groups these rows automatically (by job post and/or by status); the
        // backend's job here is just to return a well-filtered flat list efficiently.
        [HttpGet("dashboard")]
        [Authorize(Roles = "Admin,Recruiter")]
        public async Task<ActionResult<IEnumerable<JobApplicationDto>>> GetDashboard(
            [FromQuery] string? search,
            [FromQuery] ApplicationStatus? status,
            [FromQuery] bool? endorsed,
            [FromQuery] int? jobPostId)
        {
            var query = _db.JobApplications
                .Include(ja => ja.JobPost).ThenInclude(jp => jp.Company)
                .Include(ja => ja.Applicant).ThenInclude(a => a.User)
                .AsQueryable();

            if (!User.IsInRole("Admin"))
            {
                var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
                var recruiter = await _db.Recruiters.FirstOrDefaultAsync(r => r.UserId == userId);
                if (recruiter == null) return Ok(Array.Empty<JobApplicationDto>());

                query = query.Where(ja => ja.JobPost.RecruiterId == recruiter.Id);
            }

            if (jobPostId.HasValue)
                query = query.Where(ja => ja.JobPostId == jobPostId.Value);

            if (status.HasValue)
                query = query.Where(ja => ja.Status == status.Value);

            if (endorsed.HasValue)
                query = query.Where(ja => ja.EndorsedToCompany == endorsed.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(ja =>
                    ja.JobPost.Title.Contains(term) ||
                    ja.JobPost.Company.Name.Contains(term) ||
                    ja.Applicant.User.FullName.Contains(term) ||
                    ja.Applicant.User.Email.Contains(term) ||
                    (ja.RecruiterNotes != null && ja.RecruiterNotes.Contains(term)) ||
                    ja.Status.ToString().Contains(term));
            }

            var result = await query
                .OrderByDescending(ja => ja.AppliedAtUtc)
                .Select(ja => Map(ja))
                .ToListAsync();

            return Ok(result);
        }

        // POST api/jobapplications  (Applicant applies to a job post)
        [HttpPost]
        [Authorize(Roles = "Applicant")]
        public async Task<ActionResult<JobApplicationDto>> Create(CreateJobApplicationRequest request)
        {
            var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
            var applicant = await _db.Applicants.FirstOrDefaultAsync(a => a.UserId == userId);
            if (applicant == null) return Forbid();

            var jobPost = await _db.JobPosts.FindAsync(request.JobPostId);
            if (jobPost == null || jobPost.Status != JobPostStatus.Published)
                return BadRequest(new { message = "This job post is not open for applications." });

            var alreadyApplied = await _db.JobApplications
                .AnyAsync(ja => ja.JobPostId == request.JobPostId && ja.ApplicantId == applicant.Id);
            if (alreadyApplied)
                return Conflict(new { message = "You have already applied to this job post." });

            var jobApplication = new JobApplication
            {
                JobPostId = request.JobPostId,
                ApplicantId = applicant.Id,
                CoverLetter = request.CoverLetter,
                ResumeUrl = request.ResumeUrl ?? applicant.ResumeUrl,
                Status = ApplicationStatus.Submitted
            };

            _db.JobApplications.Add(jobApplication);
            await _db.SaveChangesAsync();

            await _db.Entry(jobApplication).Reference(ja => ja.JobPost).LoadAsync();
            await _db.Entry(jobApplication.JobPost).Reference(jp => jp.Company).LoadAsync();
            await _db.Entry(jobApplication).Reference(ja => ja.Applicant).LoadAsync();
            await _db.Entry(jobApplication.Applicant).Reference(a => a.User).LoadAsync();

            return CreatedAtAction(nameof(GetById), new { id = jobApplication.Id }, Map(jobApplication));
        }

        // GET api/jobapplications/5
        [HttpGet("{id}")]
        public async Task<ActionResult<JobApplicationDto>> GetById(int id)
        {
            var jobApplication = await _db.JobApplications
                .Include(ja => ja.JobPost).ThenInclude(jp => jp.Company)
                .Include(ja => ja.Applicant).ThenInclude(a => a.User)
                .FirstOrDefaultAsync(ja => ja.Id == id);

            if (jobApplication == null) return NotFound();
            if (!await UserCanViewApplicationAsync(jobApplication)) return Forbid();

            return Ok(Map(jobApplication));
        }

        // PUT api/jobapplications/5  (Recruiter/Admin: update status, endorsement, and/or remarks
        // in a single call - each field is independently optional so the UI can PATCH just what changed).
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Recruiter")]
        public async Task<ActionResult<JobApplicationDto>> Update(int id, UpdateJobApplicationRequest request)
        {
            var jobApplication = await _db.JobApplications
                .Include(ja => ja.JobPost).ThenInclude(jp => jp.Company)
                .Include(ja => ja.Applicant).ThenInclude(a => a.User)
                .FirstOrDefaultAsync(ja => ja.Id == id);

            if (jobApplication == null) return NotFound();
            if (!await UserOwnsJobPostAsync(jobApplication.JobPost)) return Forbid();

            if (request.Status.HasValue)
                jobApplication.Status = request.Status.Value;

            if (request.EndorsedToCompany.HasValue)
            {
                jobApplication.EndorsedToCompany = request.EndorsedToCompany.Value;
                jobApplication.EndorsedAtUtc = request.EndorsedToCompany.Value ? DateTime.UtcNow : null;
            }

            if (request.RecruiterNotes != null)
                jobApplication.RecruiterNotes = request.RecruiterNotes;

            jobApplication.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(Map(jobApplication));
        }

        // Kept for backward compatibility with the original status-only endpoint.
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin,Recruiter")]
        public Task<ActionResult<JobApplicationDto>> UpdateStatus(int id, UpdateJobApplicationRequest request)
            => Update(id, request);

        // DELETE api/jobapplications/5  (Applicant withdraws their own application)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Applicant")]
        public async Task<IActionResult> Withdraw(int id)
        {
            var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");
            var jobApplication = await _db.JobApplications.Include(ja => ja.Applicant)
                .FirstOrDefaultAsync(ja => ja.Id == id);

            if (jobApplication == null) return NotFound();
            if (jobApplication.Applicant.UserId != userId) return Forbid();

            jobApplication.Status = ApplicationStatus.Withdrawn;
            jobApplication.UpdatedAtUtc = DateTime.UtcNow;
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

        private async Task<bool> UserCanViewApplicationAsync(JobApplication ja)
        {
            if (User.IsInRole("Admin")) return true;

            var userId = int.Parse(User.FindFirst("sub")?.Value ?? "0");

            if (User.IsInRole("Recruiter"))
            {
                var recruiter = await _db.Recruiters.FirstOrDefaultAsync(r => r.UserId == userId);
                var jobPost = await _db.JobPosts.FindAsync(ja.JobPostId);
                return recruiter != null && jobPost != null && recruiter.Id == jobPost.RecruiterId;
            }

            return ja.Applicant.UserId == userId;
        }

        private static JobApplicationDto Map(JobApplication ja) => new()
        {
            Id = ja.Id,
            JobPostId = ja.JobPostId,
            JobPostTitle = ja.JobPost?.Title ?? string.Empty,
            CompanyName = ja.JobPost?.Company?.Name ?? string.Empty,
            JobPostStatus = ja.JobPost?.Status ?? JobPostStatus.Draft,
            ApplicantId = ja.ApplicantId,
            ApplicantName = ja.Applicant?.User?.FullName ?? string.Empty,
            ApplicantEmail = ja.Applicant?.User?.Email ?? string.Empty,
            CoverLetter = ja.CoverLetter,
            ResumeUrl = ja.ResumeUrl,
            Status = ja.Status,
            RecruiterNotes = ja.RecruiterNotes,
            EndorsedToCompany = ja.EndorsedToCompany,
            EndorsedAtUtc = ja.EndorsedAtUtc,
            AppliedAtUtc = ja.AppliedAtUtc,
            UpdatedAtUtc = ja.UpdatedAtUtc
        };
    }
}
