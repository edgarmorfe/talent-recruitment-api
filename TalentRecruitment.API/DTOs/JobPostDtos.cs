using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using TalentRecruitment.API.Models;

namespace TalentRecruitment.API.DTOs
{
    public class JobPostDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Requirements { get; set; }
        public string? Location { get; set; }
        public bool IsRemote { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public EmploymentType EmploymentType { get; set; }
        public decimal? SalaryMin { get; set; }
        public decimal? SalaryMax { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public JobPostStatus Status { get; set; }
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public int RecruiterId { get; set; }
        public string RecruiterName { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? PublishedAtUtc { get; set; }
        public DateTime? ClosingDateUtc { get; set; }
        public int ApplicationCount { get; set; }
    }

    public class CreateJobPostRequest
    {
        [Required, MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        public string? Requirements { get; set; }
        public string? Location { get; set; }
        public bool IsRemote { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public EmploymentType EmploymentType { get; set; }
        public decimal? SalaryMin { get; set; }
        public decimal? SalaryMax { get; set; }
        public DateTime? ClosingDateUtc { get; set; }

        // Admin-only: choose which company (and optionally which of its recruiters) owns this
        // post. Ignored for callers with the Recruiter role, who always post under themselves.
        public int? CompanyId { get; set; }
        public int? RecruiterId { get; set; }
    }

    public class UpdateJobPostRequest : CreateJobPostRequest
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public JobPostStatus Status { get; set; }
    }
}
