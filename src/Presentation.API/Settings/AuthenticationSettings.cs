namespace Presentation.API.Settings
{
    public class AuthenticationSettings
    {
        public string JwtKey { get; set; }

        public string JwtIssuer { get; set; }

        public string JwtAudience { get; set; }
    }
}
