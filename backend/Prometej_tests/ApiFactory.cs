using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Prometej_core.Models.efModels;
using Prometej_persistance;
using Testcontainers.PostgreSql;

namespace Prometej_tests
{
    // The real API over HTTP, against PostgreSQL in a container. One container per test class.
    public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly PostgreSqlContainer _db = new PostgreSqlBuilder("postgres:15-alpine").Build();

        public const string AdminEmail = "admin@test.local";
        public const string TeacherEmail = "teacher@test.local";
        public const string OtherTeacherEmail = "other.teacher@test.local";
        public const string Password = "Test-password-1";

        public static readonly string JwtKey = Convert.ToBase64String(
            Enumerable.Range(1, 48).Select(i => (byte)i).ToArray());

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = _db.GetConnectionString(),
                    ["Jwt:Key"] = JwtKey,
                    ["Seed:Users:0:Email"] = AdminEmail,
                    ["Seed:Users:0:Password"] = Password,
                    ["Seed:Users:0:FirstName"] = "Ana",
                    ["Seed:Users:0:LastName"] = "Admin",
                    ["Seed:Users:0:Role"] = "admin",
                    ["Seed:Users:1:Email"] = TeacherEmail,
                    ["Seed:Users:1:Password"] = Password,
                    ["Seed:Users:1:FirstName"] = "Tea",
                    ["Seed:Users:1:LastName"] = "Teacher",
                    ["Seed:Users:1:Role"] = "teacher",
                    ["Seed:Users:2:Email"] = OtherTeacherEmail,
                    ["Seed:Users:2:Password"] = Password,
                    ["Seed:Users:2:FirstName"] = "Toni",
                    ["Seed:Users:2:LastName"] = "Other",
                    ["Seed:Users:2:Role"] = "teacher",
                }));
        }

        public async Task InitializeAsync()
        {
            await _db.StartAsync();
            _ = Services; // forces the host to start: migrate and seed
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            await base.DisposeAsync();
            await _db.DisposeAsync();
        }

        // The auth cookie is Secure; a client on http://localhost would silently drop it.
        public HttpClient CreateHttpsClient() =>
            CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

        // Each client has its own cookie container, so "another user" is another client.
        public async Task<HttpClient> LoginAs(string email, string password = Password)
        {
            var client = CreateHttpsClient();
            var response = await client.PostAsJsonAsync("/api/user/login", new { email, password });
            response.EnsureSuccessStatusCode();
            return client;
        }

        public async Task<HttpClient> LoginAsNewStudent()
        {
            var email = $"student-{Guid.NewGuid():N}@test.local";
            var registration = new { firstName = "Sara", lastName = "Student", email, password = Password };
            var response = await CreateHttpsClient().PostAsJsonAsync("/api/user/register", registration);
            response.EnsureSuccessStatusCode();
            return await LoginAs(email);
        }

        // A token forged by a test has to carry the stamp its account has now.
        public Guid SessionStampOf(int userId)
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();
            return context.Users.Where(u => u.Id == userId).Select(u => u.SessionStamp).Single();
        }

        // A quiz from before the server validated them: no questions, and whatever entry code
        // it is given. The API can no longer create one, but older databases still hold some.
        public int AddLegacyQuiz(int creatorId, bool isPrivate = false, int? entryCode = null)
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();
            var quiz = new Quiz
            {
                Title = $"Stari kviz {Guid.NewGuid():N}",
                CreatorId = creatorId,
                Creator = null!,
                IsPrivate = isPrivate,
                EntryCode = entryCode,
                Questions = [],
            };
            context.Quizzes.Add(quiz);
            context.SaveChanges();
            return quiz.Id;
        }
    }
}
