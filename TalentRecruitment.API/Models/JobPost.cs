namespace TalentRecruitment.API.Models
{
    public class JobPost
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Requirements { get; set; }
        public string? Location { get; set; }
        public bool IsRemote { get; set; }
        public EmploymentType EmploymentType { get; set; }
        public decimal? SalaryMin { get; set; }
        public decimal? SalaryMax { get; set; }
        public JobPostStatus Status { get; set; } = JobPostStatus.Draft;

        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;

        public int RecruiterId { get; set; }
        public Recruiter Recruiter { get; set; } = null!;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? PublishedAtUtc { get; set; }
        public DateTime? ClosingDateUtc { get; set; }

        public ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();
    }
}
