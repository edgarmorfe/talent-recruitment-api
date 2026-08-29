using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using TalentRecruitment.API.Models;

namespace TalentRecruitment.API.DTOs
{
    public class RegisterRequest
    {
        [Required, MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public UserRole Role { get; set; } = UserRole.Applicant;

        // Required only when Role == Recruiter and the company does not yet exist.
        public string? CompanyName { get; set; }
        public int? ExistingCompanyId { get; set; }
    }

    public class LoginRequest
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class GoogleLoginRequest
    {
        // ID token returned by Google Sign-In on the frontend (google.accounts.id).
        [Required]
        public string IdToken { get; set; } = string.Empty;

        // Only used the first time a Google user signs in and needs a role assigned.
        public UserRole? Role { get; set; }
        public string? CompanyName { get; set; }
    }

    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }
        public UserDto User { get; set; } = null!;
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public UserRole Role { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public AuthProvider AuthProvider { get; set; }
        public int? RecruiterId { get; set; }
        public int? CompanyId { get; set; }
        public int? ApplicantId { get; set; }
    }

    public class ChangeUserRoleRequest
    {
        [Required]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public UserRole NewRole { get; set; }
    }

    // ---------- Admin: User & Role Management ----------

    public class UserDetailDto : UserDto
    {
        public bool IsActive { get; set; }
        public string? CompanyName { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? LastLoginUtc { get; set; }
    }

    // Admin creates a user directly (sets an initial password on their behalf).
    public class CreateUserRequest
    {
        [Required, MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public UserRole Role { get; set; } = UserRole.Applicant;

        // Required only when Role == Recruiter and the company does not yet exist.
        public string? CompanyName { get; set; }
        public int? ExistingCompanyId { get; set; }
    }

    public class UpdateUserDetailsRequest
    {
        [Required, MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
