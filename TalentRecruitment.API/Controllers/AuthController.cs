using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TalentRecruitment.API.Data;
using TalentRecruitment.API.DTOs;
using TalentRecruitment.API.Models;
using TalentRecruitment.API.Services;

namespace TalentRecruitment.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ITokenService _tokenService;
        private readonly IGoogleAuthService _googleAuthService;
        private readonly IUserProvisioningService _provisioningService;

        public AuthController(
            ApplicationDbContext db,
            ITokenService tokenService,
            IGoogleAuthService googleAuthService,
            IUserProvisioningService provisioningService)
        {
            _db = db;
            _tokenService = tokenService;
            _googleAuthService = googleAuthService;
            _provisioningService = provisioningService;
        }

        // POST api/auth/register
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
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

            return await BuildAuthResponseAsync(user);
        }

        // POST api/auth/login
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null || user.PasswordHash == null ||
                !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            if (!user.IsActive)
                return Unauthorized(new { message = "This account has been deactivated." });

            user.LastLoginUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return await BuildAuthResponseAsync(user);
        }

        // POST api/auth/google  -- SSO using Google Sign-In (ID token flow)
        [HttpPost("google")]
        public async Task<ActionResult<AuthResponse>> GoogleLogin(GoogleLoginRequest request)
        {
            GoogleUserInfo googleUser;
            try
            {
                googleUser = await _googleAuthService.ValidateIdTokenAsync(request.IdToken);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == googleUser.Email);

            if (user == null)
            {
                // First time this Google account signs in - a role must be supplied by the frontend.
                var role = request.Role ?? UserRole.Applicant;

                user = new User
                {
                    FullName = googleUser.FullName,
                    Email = googleUser.Email,
                    GoogleId = googleUser.GoogleId,
                    Role = role,
                    AuthProvider = AuthProvider.Google
                };

                _db.Users.Add(user);
                await _db.SaveChangesAsync();

                await _provisioningService.ProvisionRoleProfileAsync(user, request.CompanyName, null);
            }
            else if (user.GoogleId == null)
            {
                // Link existing local account to Google.
                user.GoogleId = googleUser.GoogleId;
            }

            if (!user.IsActive)
                return Unauthorized(new { message = "This account has been deactivated." });

            user.LastLoginUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return await BuildAuthResponseAsync(user);
        }

        // GET api/auth/me
        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<UserDto>> Me()
        {
            var userId = int.Parse(User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _db.Users
                .Include(u => u.Recruiter)
                .Include(u => u.Applicant)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return NotFound();

            return Ok(MapUser(user));
        }

        private async Task<AuthResponse> BuildAuthResponseAsync(User user)
        {
            var (token, expires) = _tokenService.GenerateToken(user);

            var fullUser = await _db.Users
                .Include(u => u.Recruiter)
                .Include(u => u.Applicant)
                .FirstAsync(u => u.Id == user.Id);

            return new AuthResponse
            {
                Token = token,
                ExpiresAtUtc = expires,
                User = MapUser(fullUser)
            };
        }

        private static UserDto MapUser(User user) => new()
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            AuthProvider = user.AuthProvider,
            RecruiterId = user.Recruiter?.Id,
            CompanyId = user.Recruiter?.CompanyId,
            ApplicantId = user.Applicant?.Id
        };
    }
}
