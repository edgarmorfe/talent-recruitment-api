namespace TalentRecruitment.API.Models
{
    // Base identity record for anyone who can log in: Admins, Recruiters, Applicants.
    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // Null when the user authenticates purely via Google SSO.
        public string? PasswordHash { get; set; }

        public UserRole Role { get; set; }
        public AuthProvider AuthProvider { get; set; } = AuthProvider.Local;

        // Populated only when AuthProvider == Google.
        public string? GoogleId { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginUtc { get; set; }

        public Recruiter? Recruiter { get; set; }
        public Applicant? Applicant { get; set; }
    }
}
