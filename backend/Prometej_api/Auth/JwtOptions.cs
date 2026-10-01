namespace Prometej_api.Auth
{
    public sealed class JwtOptions
    {
        // Base64. Comes from user secrets in development and from the environment in production.
        public string Key { get; set; } = "";
        public string Issuer { get; set; } = "";
        public string Audience { get; set; } = "";
        public int ExpiryMinutes { get; set; } = 480;

        public bool HasUsableKey()
        {
            try
            {
                return Convert.FromBase64String(Key).Length >= 32;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
