using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Prometej_api.Seed;
using Prometej_core.Auth;
using Prometej_persistance;

namespace Prometej_tests
{
    // The API as a fresh clone starts it in development: with the demo content seeded.
    public class DemoContentFactory : ApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["Seed:DemoContent"] = "true" }));
        }
    }

    // The files embedded in the API, read the way the seeder finds them.
    internal static class SeedFiles
    {
        private const string PeriodFiles = "Prometej_api.Seed.Content.periods.";
        private const string QuizFiles = "Prometej_api.Seed.Content.quizzes.";

        public static List<(int PeriodId, string Content)> Periods() =>
            Read(PeriodFiles).Select(file => (int.Parse(file.Name[..file.Name.IndexOf('-')]), file.Text)).ToList();

        public static List<JsonElement> Quizzes() =>
            Read(QuizFiles).Select(file => JsonDocument.Parse(file.Text).RootElement).ToList();

        private static IEnumerable<(string Name, string Text)> Read(string prefix)
        {
            var assembly = typeof(DemoContentSeeder).Assembly;
            foreach (var name in assembly.GetManifestResourceNames().Where(n => n.StartsWith(prefix, StringComparison.Ordinal)).Order(StringComparer.Ordinal))
            {
                using var reader = new StreamReader(assembly.GetManifestResourceStream(name)!);
                yield return (name[prefix.Length..], reader.ReadToEnd());
            }
        }
    }

    // These tests only read what was seeded, or add Quizzes under titles of their own.
    public class DemoContentTests(DemoContentFactory factory) : IClassFixture<DemoContentFactory>
    {
        private const string DemoCreatorName = "Uredništvo Prometeja";
        private static readonly int[] FullPeriods = [3, 4, 6];
        private static readonly string[] PlainElements = ["h1", "h2", "h3", "p", "ul", "ol", "li", "blockquote", "strong", "em"];

        private static int WordCount(string html) =>
            Regex.Replace(html, "<[^>]+>", " ").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

        // What a Question asks, offers and marks correct, in a file and in the API's answer alike.
        private static string Shape(JsonElement question) => string.Join(" | ",
            new[] { "questionTitle", "firstAnswer", "secondAnswer", "thirdAnswer", "fourthAnswer", "correctOption" }
                .Select(name => question.GetProperty(name).ToString()));

        private async Task<List<JsonElement>> DemoQuizzes()
        {
            var quizzes = await factory.CreateHttpsClient().GetFromJsonAsync<JsonElement>("/api/quiz/search");
            return quizzes.EnumerateArray().Where(q => q.GetProperty("creatorName").GetString() == DemoCreatorName).ToList();
        }

        private async Task<List<JsonElement>> QuestionsOf(JsonElement quiz)
        {
            var id = quiz.GetProperty("id").GetInt32();
            var opened = await factory.CreateHttpsClient().GetFromJsonAsync<JsonElement>($"/api/quiz/get/{id}");
            return opened.GetProperty("questions").EnumerateArray().ToList();
        }

        [Fact]
        public async Task Every_period_has_seed_content_that_starts_with_its_name()
        {
            var anonymous = factory.CreateHttpsClient();
            var periods = (await anonymous.GetFromJsonAsync<JsonElement>("/api/period")).EnumerateArray().ToList();

            Assert.Equal(12, periods.Count);
            foreach (var period in periods)
            {
                var id = period.GetProperty("id").GetInt32();
                var name = period.GetProperty("name").GetString();
                var content = (await anonymous.GetFromJsonAsync<JsonElement>($"/api/period/content/{id}"))
                    .GetProperty("content").GetString()!;

                Assert.StartsWith($"<h1>{name}</h1>", content);
                Assert.True(WordCount(content) >= (FullPeriods.Contains(id) ? 800 : 200), $"{name}: {WordCount(content)} words");
            }
        }

        [Fact]
        public async Task Every_seeded_period_file_is_that_periods_content()
        {
            var anonymous = factory.CreateHttpsClient();

            foreach (var (periodId, text) in SeedFiles.Periods())
            {
                var content = await anonymous.GetFromJsonAsync<JsonElement>($"/api/period/content/{periodId}");

                Assert.Equal(text, content.GetProperty("content").GetString());
            }
        }

        // What a sanitiser would allow and what Period search reads: no attributes, links or images.
        [Fact]
        public void Seed_content_uses_only_plain_elements()
        {
            foreach (var (periodId, text) in SeedFiles.Periods())
            {
                foreach (Match tag in Regex.Matches(text, "<([^>]*)>"))
                {
                    Assert.True(PlainElements.Contains(tag.Groups[1].Value.TrimStart('/')), $"Period {periodId}: {tag.Value}");
                }
            }
        }

        [Fact]
        public async Task Seed_content_is_found_by_search()
        {
            var hits = await factory.CreateHttpsClient().GetFromJsonAsync<JsonElement>("/api/period/content/search?query=senoa");

            Assert.Contains(hits.EnumerateArray(), hit => hit.GetProperty("periodName").GetString() == "Realizam");
        }

        [Fact]
        public async Task Every_seeded_quiz_file_is_a_public_quiz_of_the_demo_creator()
        {
            var files = SeedFiles.Quizzes();
            var quizzes = await DemoQuizzes();

            Assert.Equal(
                files.Select(f => f.GetProperty("quiz").GetProperty("title").GetString()).Order(),
                quizzes.Select(q => q.GetProperty("title").GetString()).Order());
            foreach (var quiz in quizzes)
            {
                Assert.False(quiz.GetProperty("isPrivate").GetBoolean());
                Assert.Equal(JsonValueKind.Null, quiz.GetProperty("entryCode").ValueKind);

                var file = files.Single(f => f.GetProperty("quiz").GetProperty("title").GetString() == quiz.GetProperty("title").GetString());
                Assert.Equal(
                    file.GetProperty("questions").EnumerateArray().Select(Shape),
                    (await QuestionsOf(quiz)).Select(Shape));
            }
        }

        [Fact]
        public async Task The_three_full_periods_each_have_a_quiz_of_eight_questions()
        {
            var quizzes = await DemoQuizzes();

            Assert.Equal(
                ["Modernizam: provjera znanja", "Realizam: provjera znanja", "Renesansa: provjera znanja"],
                quizzes.Select(q => q.GetProperty("title").GetString()).Order());
            foreach (var quiz in quizzes)
            {
                var questions = await QuestionsOf(quiz);

                Assert.Equal(8, questions.Count);
                Assert.All(questions, q => Assert.False(string.IsNullOrWhiteSpace(q.GetProperty("exploreMore").GetString())));
                Assert.True(questions.Count(q => q.GetProperty("hintText").ValueKind == JsonValueKind.String) >= 3);
                // The right answer is not always in the same place.
                Assert.Equal(4, questions.Select(q => q.GetProperty("correctOption").GetInt32()).Distinct().Count());
            }
        }

        [Fact]
        public async Task Each_demo_quiz_is_linked_to_the_period_it_is_about()
        {
            var quizzes = await DemoQuizzes();
            var periods = await factory.CreateHttpsClient().GetFromJsonAsync<JsonElement>("/api/period");

            // In curriculum order, which is not the order of the files' names.
            Assert.Equal(["Renesansa", "Realizam", "Modernizam"], quizzes.Select(q => q.GetProperty("periodName").GetString()));
            Assert.All(quizzes, q => Assert.Equal(
                $"{q.GetProperty("periodName").GetString()}: provjera znanja", q.GetProperty("title").GetString()));
            foreach (var period in periods.EnumerateArray().Where(p => FullPeriods.Contains(p.GetProperty("id").GetInt32())))
            {
                Assert.True(period.GetProperty("quizCount").GetInt32() >= 1, period.GetProperty("name").GetString());
            }
        }

        // The seeder calls the service, which does not run the request models' validation.
        [Fact]
        public async Task A_seeded_quiz_file_is_accepted_by_the_api()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);

            foreach (var file in SeedFiles.Quizzes())
            {
                var body = new
                {
                    quiz = new
                    {
                        title = $"Provjera datoteke {Guid.NewGuid():N}",
                        isPrivate = false,
                        periodId = file.GetProperty("quiz").GetProperty("periodId").GetInt32(),
                    },
                    questions = file.GetProperty("questions"),
                };

                var response = await teacher.PostAsJsonAsync("/api/quiz/create", body);

                Assert.Equal(HttpStatusCode.Created, response.StatusCode);
                Assert.InRange(file.GetProperty("quiz").GetProperty("title").GetString()!.Length, 1, 100);
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData("!")]
        [InlineData("password")]
        public async Task Nobody_can_sign_in_as_the_demo_creator(string password)
        {
            var response = await factory.CreateHttpsClient().PostAsJsonAsync("/api/user/login",
                new { email = DemoCreator.Email, password });

            Assert.Contains(response.StatusCode, new[] { HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized });
        }

        [Fact]
        public async Task The_demo_creator_is_not_an_account_an_admin_manages()
        {
            var admin = await factory.LoginAs(ApiFactory.AdminEmail);
            int creatorId;
            using (var scope = factory.Services.CreateScope())
            {
                creatorId = scope.ServiceProvider.GetRequiredService<DataContext>().Users.Single(u => u.Email == DemoCreator.Email).Id;
            }

            var users = await admin.GetFromJsonAsync<JsonElement>("/api/user");
            var roleChange = await admin.PutAsJsonAsync($"/api/user/{creatorId}/role", new { role = "student" });

            Assert.DoesNotContain(users.EnumerateArray(), u => u.GetProperty("email").GetString() == DemoCreator.Email);
            Assert.Equal(HttpStatusCode.NotFound, roleChange.StatusCode);
        }
    }

    // In a class of its own, so its edits are made in a database no other test reads.
    public class DemoContentReseedTests(DemoContentFactory factory) : IClassFixture<DemoContentFactory>
    {
        private (int Users, int Quizzes, int PeriodContents) Reseed()
        {
            using var scope = factory.Services.CreateScope();
            DemoContentSeeder.Run(scope.ServiceProvider, scope.ServiceProvider.GetRequiredService<IConfiguration>());

            var db = scope.ServiceProvider.GetRequiredService<DataContext>();
            return (db.Users.Count(), db.Quizzes.Count(), db.PeriodContents.Count());
        }

        [Fact]
        public async Task Seeding_again_adds_nothing_and_keeps_what_an_admin_changed()
        {
            var admin = await factory.LoginAs(ApiFactory.AdminEmail);
            var before = Reseed();

            var edit = await admin.PostAsJsonAsync("/api/period/content", new { id = 0, periodId = 12, content = "<p>Uredio admin</p>" });
            edit.EnsureSuccessStatusCode();
            var quizzes = await admin.GetFromJsonAsync<JsonElement>("/api/quiz/search");
            var demoQuiz = quizzes.EnumerateArray().First(q => q.GetProperty("creatorName").GetString() == "Uredništvo Prometeja");
            var delete = await admin.DeleteAsync($"/api/quiz/delete/{demoQuiz.GetProperty("id").GetInt32()}");
            delete.EnsureSuccessStatusCode();

            var after = Reseed();

            // The deleted Quiz stays deleted: the Quizzes are only seeded while their Creator has none.
            Assert.Equal(before with { Quizzes = before.Quizzes - 1 }, after);
            var content = await admin.GetFromJsonAsync<JsonElement>("/api/period/content/12");
            Assert.Equal("<p>Uredio admin</p>", content.GetProperty("content").GetString());
        }
    }

    public class NoDemoContentTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        [Fact]
        public async Task Nothing_is_seeded_unless_demo_content_is_asked_for()
        {
            var anonymous = factory.CreateHttpsClient();

            var content = await anonymous.GetAsync("/api/period/content/1");
            var quizzes = await anonymous.GetFromJsonAsync<JsonElement>("/api/quiz/search");

            Assert.Equal(HttpStatusCode.NotFound, content.StatusCode);
            Assert.DoesNotContain(quizzes.EnumerateArray(), q => q.GetProperty("creatorName").GetString() == "Uredništvo Prometeja");
            using var scope = factory.Services.CreateScope();
            Assert.False(scope.ServiceProvider.GetRequiredService<DataContext>().Users.Any(u => u.Email == DemoCreator.Email));
        }
    }
}
