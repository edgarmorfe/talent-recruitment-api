using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using TalentRecruitment.API.Models;

namespace TalentRecruitment.API.DTOs
{
    public class JobApplicationDto
    {
        public int Id { get; set; }
        public int JobPostId { get; set; }
        public string JobPostTitle { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public JobPostStatus JobPostStatus { get; set; }
        public int ApplicantId { get; set; }
        public string ApplicantName { get; set; } = string.Empty;
        public string ApplicantEmail { get; set; } = string.Empty;
        public string? CoverLetter { get; set; }
        public string? ResumeUrl { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ApplicationStatus Status { get; set; }
        public string? RecruiterNotes { get; set; }
        public bool EndorsedToCompany { get; set; }
        public DateTime? EndorsedAtUtc { get; set; }
        public DateTime AppliedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }

    public class CreateJobApplicationRequest
    {
        [Required]
        public int JobPostId { get; set; }
        public string? CoverLetter { get; set; }
        public string? ResumeUrl { get; set; }
    }

    // Used by recruiters/admins to move an applicant through the pipeline, endorse them to the
    // hiring company, and/or leave remarks. All fields are optional so the frontend can PATCH
    // just the piece that changed (e.g. toggling the endorsement switch shouldn't require resending status).
    public class UpdateJobApplicationRequest
    {
        public ApplicationStatus? Status { get; set; }
        public bool? EndorsedToCompany { get; set; }
        public string? RecruiterNotes { get; set; }
    }
}
