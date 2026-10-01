namespace Prometej_core.Auth
{
    public static class PasswordHasher
    {
        private const int WorkFactor = 11;

        public static string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

        public static bool Verify(string password, string hash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                // The stored value is not a bcrypt hash. Nothing can match it.
                return false;
            }
        }
    }
}
