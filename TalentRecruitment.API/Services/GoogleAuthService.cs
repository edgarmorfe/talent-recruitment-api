using Google.Apis.Auth;

namespace TalentRecruitment.API.Services
{
    // Validates the ID token issued by Google Sign-In (google.accounts.id) on the React frontend.
    public class GoogleAuthService : IGoogleAuthService
    {
        private readonly IConfiguration _config;

        public GoogleAuthService(IConfiguration config)
        {
            _config = config;
        }

        public async Task<GoogleUserInfo> ValidateIdTokenAsync(string idToken)
        {
            var clientId = _config["Google:ClientId"];

            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { clientId }
            };

            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
            }
            catch (InvalidJwtException ex)
            {
                throw new UnauthorizedAccessException("Invalid Google ID token.", ex);
            }

            return new GoogleUserInfo
            {
                GoogleId = payload.Subject,
                Email = payload.Email,
                FullName = payload.Name ?? payload.Email
            };
        }
    }
}
