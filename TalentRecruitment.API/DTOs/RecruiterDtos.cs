namespace TalentRecruitment.API.DTOs
{
    public class RecruiterDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? JobTitle { get; set; }
        public string? PhoneNumber { get; set; }
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
    }

    public class UpdateRecruiterRequest
    {
        public string? JobTitle { get; set; }
        public string? PhoneNumber { get; set; }
    }
}
