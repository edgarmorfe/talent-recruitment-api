using TalentRecruitment.API.Models;

namespace TalentRecruitment.API.Data
{
    // Seeds a minimal working dataset so the app is usable immediately after `dotnet ef database update`.
    public static class DbInitializer
    {
        public static void Seed(ApplicationDbContext context)
        {
            context.Database.EnsureCreated();

            if (context.Users.Any()) return; // already seeded

            var admin = new User
            {
                FullName = "System Admin",
                Email = "admin@talentsaas.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                Role = UserRole.Admin,
                AuthProvider = AuthProvider.Local
            };

            var company = new Company
            {
                Name = "Acme Corporation",
                Description = "A sample company for demo purposes.",
                Industry = "Technology",
                Location = "Manila, PH",
                Website = "https://acme.example.com"
            };

            var recruiterUser = new User
            {
                FullName = "Rita Recruiter",
                Email = "recruiter@talentsaas.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Recruiter@123"),
                Role = UserRole.Recruiter,
                AuthProvider = AuthProvider.Local
            };

            var recruiter = new Recruiter
            {
                User = recruiterUser,
                Company = company,
                JobTitle = "Senior Talent Acquisition Specialist"
            };

            var applicantUser = new User
            {
                FullName = "Alex Applicant",
                Email = "applicant@talentsaas.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Applicant@123"),
                Role = UserRole.Applicant,
                AuthProvider = AuthProvider.Local
            };

            var applicant = new Applicant
            {
                User = applicantUser,
                Skills = "C#, React, SQL",
                Education = "BS Computer Science",
                YearsOfExperience = 3,
                Location = "Taguig, PH"
            };

            context.Users.AddRange(admin, recruiterUser, applicantUser);
            context.Companies.Add(company);
            context.Recruiters.Add(recruiter);
            context.Applicants.Add(applicant);
            context.SaveChanges();

            var jobPost = new JobPost
            {
                Title = "Full Stack Developer (.NET + React)",
                Description = "Build and maintain features across our talent platform.",
                Requirements = "3+ years C#, ASP.NET Core, React, SQL Server.",
                Location = "Manila, PH",
                IsRemote = true,
                EmploymentType = EmploymentType.FullTime,
                SalaryMin = 80000,
                SalaryMax = 120000,
                Status = JobPostStatus.Published,
                Company = company,
                Recruiter = recruiter,
                PublishedAtUtc = DateTime.UtcNow
            };

            var secondJobPost = new JobPost
            {
                Title = "QA Engineer",
                Description = "Own manual and automated testing for our recruitment platform.",
                Requirements = "2+ years QA, Selenium/Playwright, SQL basics.",
                Location = "Cebu, PH",
                IsRemote = false,
                EmploymentType = EmploymentType.FullTime,
                SalaryMin = 50000,
                SalaryMax = 75000,
                Status = JobPostStatus.Published,
                Company = company,
                Recruiter = recruiter,
                PublishedAtUtc = DateTime.UtcNow
            };

            context.JobPosts.AddRange(jobPost, secondJobPost);
            context.SaveChanges();

            // A second applicant + a couple of applications, so the Recruiter Dashboard has
            // something to group/search over right after the first `dotnet run`.
            var secondApplicantUser = new User
            {
                FullName = "Jamie Jobseeker",
                Email = "jamie@talentsaas.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Applicant@123"),
                Role = UserRole.Applicant,
                AuthProvider = AuthProvider.Local
            };
            var secondApplicant = new Applicant
            {
                User = secondApplicantUser,
                Skills = "Manual Testing, Selenium, SQL",
                Education = "BS Information Technology",
                YearsOfExperience = 2,
                Location = "Cebu, PH"
            };

            context.Users.Add(secondApplicantUser);
            context.Applicants.Add(secondApplicant);
            context.SaveChanges();

            context.JobApplications.AddRange(
                new JobApplication
                {
                    JobPost = jobPost,
                    Applicant = applicant,
                    CoverLetter = "I'd love to bring my full-stack experience to this role.",
                    Status = ApplicationStatus.UnderReview
                },
                new JobApplication
                {
                    JobPost = secondJobPost,
                    Applicant = secondApplicant,
                    CoverLetter = "QA is my passion - I've automated test suites for two prior teams.",
                    Status = ApplicationStatus.Shortlisted,
                    EndorsedToCompany = true,
                    EndorsedAtUtc = DateTime.UtcNow,
                    RecruiterNotes = "Strong Selenium background, moving to interview."
                }
            );
            context.SaveChanges();
        }
    }
}
