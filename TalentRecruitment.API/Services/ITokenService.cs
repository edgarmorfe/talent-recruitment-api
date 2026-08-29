using TalentRecruitment.API.Models;

namespace TalentRecruitment.API.Services
{
    public interface ITokenService
    {
        (string Token, DateTime ExpiresAtUtc) GenerateToken(User user);
    }
}
