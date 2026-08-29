namespace TalentRecruitment.API.Models
{
    public class Company
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Website { get; set; }
        public string? Industry { get; set; }
        public string? LogoUrl { get; set; }
        public string? Location { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public ICollection<Recruiter> Recruiters { get; set; } = new List<Recruiter>();
        public ICollection<JobPost> JobPosts { get; set; } = new List<JobPost>();
    }
}
