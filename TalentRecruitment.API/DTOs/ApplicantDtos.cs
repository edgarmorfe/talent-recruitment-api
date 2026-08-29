namespace TalentRecruitment.API.DTOs
{
    public class ApplicantDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? ResumeUrl { get; set; }
        public string? Skills { get; set; }
        public string? Education { get; set; }
        public int? YearsOfExperience { get; set; }
        public string? Location { get; set; }
        public string? LinkedInUrl { get; set; }
    }

    public class UpdateApplicantRequest
    {
        public string? PhoneNumber { get; set; }
        public string? ResumeUrl { get; set; }
        public string? Skills { get; set; }
        public string? Education { get; set; }
        public int? YearsOfExperience { get; set; }
        public string? Location { get; set; }
        public string? LinkedInUrl { get; set; }
    }
}
