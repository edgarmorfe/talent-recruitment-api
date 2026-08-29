using System.ComponentModel.DataAnnotations;

namespace TalentRecruitment.API.DTOs
{
    public class CompanyDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Website { get; set; }
        public string? Industry { get; set; }
        public string? LogoUrl { get; set; }
        public string? Location { get; set; }
        public int JobPostCount { get; set; }
        public int RecruiterCount { get; set; }
    }

    public class CreateCompanyRequest
    {
        [Required, MaxLength(300)]
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Website { get; set; }
        public string? Industry { get; set; }
        public string? LogoUrl { get; set; }
        public string? Location { get; set; }
    }

    public class UpdateCompanyRequest : CreateCompanyRequest { }
}
