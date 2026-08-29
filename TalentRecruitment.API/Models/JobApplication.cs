namespace TalentRecruitment.API.Models
{
    public class JobApplication
    {
        public int Id { get; set; }

        public int JobPostId { get; set; }
        public JobPost JobPost { get; set; } = null!;

        public int ApplicantId { get; set; }
        public Applicant Applicant { get; set; } = null!;

        public string? CoverLetter { get; set; }
        public string? ResumeUrl { get; set; }
        public ApplicationStatus Status { get; set; } = ApplicationStatus.Submitted;

        // Recruiter-facing pipeline fields.
        public string? RecruiterNotes { get; set; }     // shown to the recruiter as "Remarks"
        public bool EndorsedToCompany { get; set; }      // recruiter has forwarded this applicant to the hiring company
        public DateTime? EndorsedAtUtc { get; set; }

        public DateTime AppliedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
