using TalentRecruitment.API.Data;
using TalentRecruitment.API.Models;

namespace TalentRecruitment.API.Services
{
    public class UserProvisioningService : IUserProvisioningService
    {
        private readonly ApplicationDbContext _db;

        public UserProvisioningService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task ProvisionRoleProfileAsync(User user, string? companyName, int? existingCompanyId)
        {
            if (user.Role == UserRole.Applicant)
            {
                _db.Applicants.Add(new Applicant { UserId = user.Id });
                await _db.SaveChangesAsync();
            }
            else if (user.Role == UserRole.Recruiter)
            {
                Company company;

                if (existingCompanyId.HasValue)
                {
                    company = await _db.Companies.FindAsync(existingCompanyId.Value)
                        ?? throw new InvalidOperationException("Company not found.");
                }
                else
                {
                    company = new Company { Name = companyName ?? $"{user.FullName}'s Company" };
                    _db.Companies.Add(company);
                    await _db.SaveChangesAsync();
                }

                _db.Recruiters.Add(new Recruiter { UserId = user.Id, CompanyId = company.Id });
                await _db.SaveChangesAsync();
            }
        }
    }
}
