namespace TalentRecruitment.API.Models
{
    public class Applicant
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public string? PhoneNumber { get; set; }
        public string? ResumeUrl { get; set; }
        public string? Skills { get; set; }
        public string? Education { get; set; }
        public int? YearsOfExperience { get; set; }
        public string? Location { get; set; }
        public string? LinkedInUrl { get; set; }

        public ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();
    }
}
