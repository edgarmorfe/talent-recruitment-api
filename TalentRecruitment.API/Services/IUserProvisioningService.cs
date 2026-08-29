using TalentRecruitment.API.Models;

namespace TalentRecruitment.API.Services
{
    public interface IUserProvisioningService
    {
        // Creates the Recruiter or Applicant profile row that belongs to a freshly created User,
        // based on its Role. Applicants get an empty profile; Recruiters get attached to an
        // existing company (ExistingCompanyId) or a brand new one (CompanyName).
        Task ProvisionRoleProfileAsync(User user, string? companyName, int? existingCompanyId);
    }
}
