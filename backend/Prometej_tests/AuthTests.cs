using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Prometej_tests
{
    public class AuthTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        private const string StudentPassword = "student-pass-1";

        private static string NewEmail() => $"student-{Guid.NewGuid():N}@test.local";

        private static object Registration(string email, string password = StudentPassword) =>
            new { firstName = "Sara", lastName = "Student", email, password };

        private async Task<string> RegisterStudent()
        {
            var email = NewEmail();
            var response = await factory.CreateHttpsClient().PostAsJsonAsync("/api/user/register", Registration(email));
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return email;
        }

        private static string CreateToken(string key, DateTime expires) =>
            new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
            {
                Issuer = "prometej",
                Audience = "prometej",
                NotBefore = expires.AddHours(-2),
                IssuedAt = expires.AddHours(-2),
                Expires = expires,
                Claims = new Dictionary<string, object> { ["sub"] = "1", ["role"] = "admin" },
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(Convert.FromBase64String(key)), SecurityAlgorithms.HmacSha256),
            });

        private async Task<HttpStatusCode> GetMeWithToken(string token)
        {
            var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                HandleCookies = false,
            });
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/user/me");
            request.Headers.Add("Cookie", $"prometej_auth={token}");
            return (await client.SendAsync(request)).StatusCode;
        }

        [Fact]
        public async Task Register_always_creates_a_student_whatever_role_the_body_asks_for()
        {
            var email = NewEmail();
            var body = new { firstName = "Sara", lastName = "Student", email, password = StudentPassword, role = "admin" };

            var response = await factory.CreateHttpsClient().PostAsJsonAsync("/api/user/register", body);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var client = await factory.LoginAs(email, StudentPassword);
            var me = await client.GetFromJsonAsync<JsonElement>("/api/user/me");
            Assert.Equal("student", me.GetProperty("role").GetString());
        }

        [Fact]
        public async Task Register_rejects_an_email_that_differs_only_in_case_or_spacing()
        {
            var email = await RegisterStudent();

            var response = await factory.CreateHttpsClient()
                .PostAsJsonAsync("/api/user/register", Registration($" {email.ToUpperInvariant()} "));

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task Register_rejects_a_password_shorter_than_eight_characters()
        {
            var response = await factory.CreateHttpsClient()
                .PostAsJsonAsync("/api/user/register", Registration(NewEmail(), "1234567"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Login_sets_an_http_only_secure_lax_cookie()
        {
            var email = await RegisterStudent();

            var response = await factory.CreateHttpsClient()
                .PostAsJsonAsync("/api/user/login", new { email, password = StudentPassword });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie")).ToLowerInvariant();
            Assert.StartsWith("prometej_auth=", cookie);
            Assert.Contains("httponly", cookie);
            Assert.Contains("secure", cookie);
            Assert.Contains("samesite=lax", cookie);
        }

        [Fact]
        public async Task Login_answers_a_wrong_password_and_an_unknown_email_identically()
        {
            var email = await RegisterStudent();
            var client = factory.CreateHttpsClient();

            var wrongPassword = await client.PostAsJsonAsync("/api/user/login", new { email, password = "wrong-password" });
            var unknownEmail = await client.PostAsJsonAsync("/api/user/login", new { email = NewEmail(), password = StudentPassword });

            Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);
            Assert.Equal(
                await wrongPassword.Content.ReadAsStringAsync(),
                await unknownEmail.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Seeded_admin_can_log_in()
        {
            var client = await factory.LoginAs(ApiFactory.AdminEmail);

            var me = await client.GetFromJsonAsync<JsonElement>("/api/user/me");

            Assert.Equal("admin", me.GetProperty("role").GetString());
        }

        [Fact]
        public async Task Me_requires_a_session()
        {
            var response = await factory.CreateHttpsClient().GetAsync("/api/user/me");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Me_returns_the_caller_without_any_password_field()
        {
            var email = await RegisterStudent();
            var client = await factory.LoginAs(email, StudentPassword);

            var response = await client.GetAsync("/api/user/me");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var json = await response.Content.ReadAsStringAsync();
            Assert.Contains(email, json);
            Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Logout_ends_the_session_and_can_be_repeated()
        {
            var email = await RegisterStudent();
            var client = await factory.LoginAs(email, StudentPassword);

            var first = await client.PostAsync("/api/user/logout", null);
            var second = await client.PostAsync("/api/user/logout", null);

            Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/user/me")).StatusCode);
        }

        [Fact]
        public async Task The_endpoint_that_listed_every_user_is_gone()
        {
            var client = await factory.LoginAs(ApiFactory.AdminEmail);

            var response = await client.GetAsync("/api/user");

            Assert.Contains(response.StatusCode, new[] { HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed });
        }

        [Fact]
        public async Task Deleting_the_account_ends_every_session_and_the_login()
        {
            var email = await RegisterStudent();
            var client = await factory.LoginAs(email, StudentPassword);
            var otherDevice = await factory.LoginAs(email, StudentPassword);

            var response = await client.DeleteAsync("/api/user/me");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            // The other device still holds a validly signed token for a user that no longer exists.
            Assert.Equal(HttpStatusCode.Unauthorized, (await otherDevice.GetAsync("/api/user/me")).StatusCode);
            var login = await factory.CreateHttpsClient()
                .PostAsJsonAsync("/api/user/login", new { email, password = StudentPassword });
            Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        }

        [Fact]
        public async Task A_token_signed_with_another_key_is_rejected()
        {
            var otherKey = Convert.ToBase64String(Enumerable.Repeat((byte)7, 48).ToArray());

            Assert.Equal(HttpStatusCode.Unauthorized, await GetMeWithToken(CreateToken(otherKey, DateTime.UtcNow.AddHours(1))));
        }

        [Fact]
        public async Task An_expired_token_is_rejected()
        {
            Assert.Equal(HttpStatusCode.Unauthorized, await GetMeWithToken(CreateToken(ApiFactory.JwtKey, DateTime.UtcNow.AddHours(-1))));
        }

        [Fact]
        public async Task A_correctly_signed_token_is_accepted()
        {
            // The control for the two tests above: the same forged token passes authentication
            // once the key and the lifetime are right. User 1 is the seeded admin.
            Assert.Equal(HttpStatusCode.OK, await GetMeWithToken(CreateToken(ApiFactory.JwtKey, DateTime.UtcNow.AddHours(1))));
        }
    }
}
