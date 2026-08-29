namespace TalentRecruitment.API.Services
{
    public class GoogleUserInfo
    {
        public string GoogleId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }

    public interface IGoogleAuthService
    {
        Task<GoogleUserInfo> ValidateIdTokenAsync(string idToken);
    }
}
