namespace TalentRecruitment.API.Models
{
    public enum UserRole
    {
        Admin = 0,
        Recruiter = 1,
        Applicant = 2
    }

    public enum JobPostStatus
    {
        Draft = 0,
        Published = 1,
        Closed = 2
    }

    public enum EmploymentType
    {
        FullTime = 0,
        PartTime = 1,
        Contract = 2,
        Internship = 3,
        Temporary = 4
    }

    public enum ApplicationStatus
    {
        Submitted = 0,
        UnderReview = 1,
        Shortlisted = 2,
        InterviewScheduled = 3,
        Offered = 4,
        Rejected = 5,
        Withdrawn = 6,
        Hired = 7
    }

    public enum AuthProvider
    {
        Local = 0,
        Google = 1
    }
}
