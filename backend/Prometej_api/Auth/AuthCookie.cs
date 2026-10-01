namespace Prometej_api.Auth
{
    // The session token travels in an HttpOnly cookie, so script on the page can never read it.
    public static class AuthCookie
    {
        public const string Name = "prometej_auth";

        public static void Append(HttpResponse response, string token, DateTimeOffset expires) =>
            response.Cookies.Append(Name, token, Options(expires));

        public static void Delete(HttpResponse response) =>
            response.Cookies.Delete(Name, Options(null));

        private static CookieOptions Options(DateTimeOffset? expires) => new()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = expires,
        };
    }
}
