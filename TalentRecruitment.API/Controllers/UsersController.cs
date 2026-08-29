using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TalentRecruitment.API.Data;
using TalentRecruitment.API.DTOs;
using TalentRecruitment.API.Models;
using TalentRecruitment.API.Services;

namespace TalentRecruitment.API.Controllers
{
    // Central place for User & Role Management - Admin-only, full CRUD.
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IUserProvisioningService _provisioningService;

        public UsersController(ApplicationDbContext db, IUserProvisioningService provisioningService)
        {
            _db = db;
            _provisioningService = provisioningService;
        }

        // GET api/users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDetailDto>>> GetAll()
        {
            var users = await _db.Users
                .Include(u => u.Recruiter).ThenInclude(r => r!.Company)
                .Include(u => u.Applicant)
                .OrderBy(u => u.FullName)
                .ToListAsync();

            return Ok(users.Select(Map));
        }

        // GET api/users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserDetailDto>> GetById(int id)
        {
            var user = await _db.Users
                .Include(u => u.Recruiter).ThenInclude(r => r!.Company)
                .Include(u => u.Applicant)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return NotFound();
            return Ok(Map(user));
        }

        // POST api/users  (Admin creates a user account directly)
        [HttpPost]
        public async Task<ActionResult<UserDetailDto>> Create(CreateUserRequest request)
        {
            if (await _db.Users.AnyAsync(u => u.Email == request.Email))
                return Conflict(new { message = "A user with this email already exists." });

            var user = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = request.Role,
                AuthProvider = AuthProvider.Local
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            await _provisioningService.ProvisionRoleProfileAsync(user, request.CompanyName, request.ExistingCompanyId);

            var created = await _db.Users
                .Include(u => u.Recruiter).ThenInclude(r => r!.Company)
                .Include(u => u.Applicant)
                .FirstAsync(u => u.Id == user.Id);

            return CreatedAtAction(nameof(GetById), new { id = user.Id }, Map(created));
        }

        // PUT api/users/5  (edit name/email)
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateUserDetailsRequest request)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            if (await _db.Users.AnyAsync(u => u.Id != id && u.Email == request.Email))
                return Conflict(new { message = "Another user already uses this email." });

            user.FullName = request.FullName;
            user.Email = request.Email;
            await _db.SaveChangesAsync();

            return NoContent();
        }

        // PUT api/users/5/role
        [HttpPut("{id}/role")]
        public async Task<IActionResult> ChangeRole(int id, ChangeUserRoleRequest request)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.Role = request.NewRole;
            await _db.SaveChangesAsync();

            return NoContent();
        }

        // PUT api/users/5/deactivate
        [HttpPut("{id}/deactivate")]
        public async Task<IActionResult> Deactivate(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.IsActive = false;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // PUT api/users/5/reactivate
        [HttpPut("{id}/reactivate")]
        public async Task<IActionResult> Reactivate(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.IsActive = true;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // DELETE api/users/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            _db.Users.Remove(user);
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Most commonly: a Recruiter with existing job posts (Job Post -> Recruiter is a
                // restricted FK, by design, so history isn't silently orphaned).
                return Conflict(new
                {
                    message = "This user cannot be deleted because related records (e.g. job posts) still reference them. Deactivate the account instead, or remove those records first."
                });
            }

            return NoContent();
        }

        private static UserDetailDto Map(User u) => new()
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email,
            Role = u.Role,
            AuthProvider = u.AuthProvider,
            RecruiterId = u.Recruiter?.Id,
            CompanyId = u.Recruiter?.CompanyId,
            ApplicantId = u.Applicant?.Id,
            IsActive = u.IsActive,
            CompanyName = u.Recruiter?.Company?.Name,
            CreatedAtUtc = u.CreatedAtUtc,
            LastLoginUtc = u.LastLoginUtc
        };
    }
}
