using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Prometej_tests
{
    // Each test registers the student whose account it changes.
    public class AccountTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        private static string NewEmail() => $"student-{Guid.NewGuid():N}@test.local";

        private async Task<string> Register()
        {
            var email = NewEmail();
            var registration = new { firstName = "Sara", lastName = "Student", email, password = ApiFactory.Password };
            var response = await factory.CreateHttpsClient().PostAsJsonAsync("/api/user/register", registration);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return email;
        }

        private static Task<JsonElement> Me(HttpClient client) =>
            client.GetFromJsonAsync<JsonElement>("/api/user/me");

        private static Task<HttpResponseMessage> ChangePassword(HttpClient client, string currentPassword, string newPassword) =>
            client.PutAsJsonAsync("/api/user/me/password", new { currentPassword, newPassword });

        private async Task<HttpStatusCode> Login(string email, string password) =>
            (await factory.CreateHttpsClient().PostAsJsonAsync("/api/user/login", new { email, password })).StatusCode;

        private static async Task<int> CreateQuiz(HttpClient client)
        {
            var body = new
            {
                quiz = new { title = "Kviz", isPrivate = false },
                questions = new[]
                {
                    new { questionTitle = "Pitanje", firstAnswer = "A", secondAnswer = "B", thirdAnswer = "C", fourthAnswer = "D", correctOption = 1 },
                },
            };
            var response = await client.PostAsJsonAsync("/api/quiz/create", body);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return await response.Content.ReadFromJsonAsync<int>();
        }

        private static async Task Play(HttpClient client, int quizId)
        {
            var quiz = await client.GetFromJsonAsync<JsonElement>($"/api/quiz/get/{quizId}");
            var answers = quiz.GetProperty("questions").EnumerateArray()
                .Select(q => new { questionId = q.GetProperty("id").GetInt32(), chosenOption = 1 });
            var response = await client.PostAsJsonAsync("/api/quiz/submit", new { quizId, answers });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task A_user_changes_their_name_and_it_is_trimmed()
        {
            var email = await Register();
            var client = await factory.LoginAs(email);

            var response = await client.PutAsJsonAsync("/api/user/me", new { firstName = "  Nova ", lastName = " Imenić " });

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            var me = await Me(client);
            Assert.Equal("Nova", me.GetProperty("firstName").GetString());
            Assert.Equal("Imenić", me.GetProperty("lastName").GetString());
            Assert.Equal(email, me.GetProperty("email").GetString());
            Assert.Equal("student", me.GetProperty("role").GetString());
        }

        [Fact]
        public async Task A_name_must_be_present_and_at_most_100_characters()
        {
            var client = await factory.LoginAs(await Register());
            object[] names =
            [
                new { firstName = "", lastName = "Student" },
                new { firstName = "Sara", lastName = "   " },
                new { firstName = new string('a', 101), lastName = "Student" },
                new { firstName = "Sara" },
            ];

            foreach (var name in names)
            {
                var response = await client.PutAsJsonAsync("/api/user/me", name);

                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            }

            var me = await Me(client);
            Assert.Equal("Sara", me.GetProperty("firstName").GetString());
            Assert.Equal("Student", me.GetProperty("lastName").GetString());
        }

        // The only test of this class that renames a seeded account.
        [Fact]
        public async Task A_changed_name_shows_on_the_users_quiz_games_and_quizzes()
        {
            var teacher = await factory.LoginAs(ApiFactory.OtherTeacherEmail);
            var student = await factory.LoginAs(await Register());
            var quizId = await CreateQuiz(teacher);
            await Play(student, quizId);

            var studentRename = await student.PutAsJsonAsync("/api/user/me", new { firstName = "Nova", lastName = "Imenić" });
            var teacherRename = await teacher.PutAsJsonAsync("/api/user/me", new { firstName = "Tonka", lastName = "Druga" });

            Assert.Equal(HttpStatusCode.NoContent, studentRename.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, teacherRename.StatusCode);
            var analytics = await teacher.GetFromJsonAsync<JsonElement>($"/api/quiz/getAnalytics/{quizId}");
            Assert.Equal("Nova Imenić", analytics.GetProperty("games")[0].GetProperty("userName").GetString());
            Assert.Equal("Nova Imenić", analytics.GetProperty("players")[0].GetProperty("userName").GetString());
            var quizzes = await student.GetFromJsonAsync<JsonElement>("/api/quiz/search");
            var quiz = quizzes.EnumerateArray().Single(q => q.GetProperty("id").GetInt32() == quizId);
            Assert.Equal("Tonka Druga", quiz.GetProperty("creatorName").GetString());
        }

        [Fact]
        public async Task A_password_change_needs_the_current_password()
        {
            var email = await Register();
            var client = await factory.LoginAs(email);

            var response = await ChangePassword(client, "wrong-password", "New-password-1");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/user/me")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, await Login(email, ApiFactory.Password));
        }

        [Fact]
        public async Task A_password_change_ends_every_other_session_and_keeps_this_one()
        {
            var email = await Register();
            var client = await factory.LoginAs(email);
            var otherDevice = await factory.LoginAs(email);

            var response = await ChangePassword(client, ApiFactory.Password, "New-password-1");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.StartsWith("prometej_auth=", Assert.Single(response.Headers.GetValues("Set-Cookie")));
            // The client keeps cookies, so it already holds the one this response set.
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/user/me")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await otherDevice.GetAsync("/api/user/me")).StatusCode);
        }

        [Fact]
        public async Task After_a_password_change_only_the_new_password_signs_in()
        {
            var email = await Register();
            var client = await factory.LoginAs(email);

            var response = await ChangePassword(client, ApiFactory.Password, "New-password-1");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, await Login(email, ApiFactory.Password));
            Assert.Equal(HttpStatusCode.OK, await Login(email, "New-password-1"));
        }

        [Fact]
        public async Task A_new_password_has_registrations_limits()
        {
            var email = await Register();
            var client = await factory.LoginAs(email);

            var tooShort = await ChangePassword(client, ApiFactory.Password, "1234567");
            var tooLong = await ChangePassword(client, ApiFactory.Password, new string('a', 73));

            Assert.Equal(HttpStatusCode.BadRequest, tooShort.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, tooLong.StatusCode);
            Assert.Equal(HttpStatusCode.OK, await Login(email, ApiFactory.Password));
        }

        [Fact]
        public async Task A_new_password_may_equal_the_current_one()
        {
            var email = await Register();
            var client = await factory.LoginAs(email);
            var otherDevice = await factory.LoginAs(email);

            var response = await ChangePassword(client, ApiFactory.Password, ApiFactory.Password);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            // The stamp changed all the same.
            Assert.Equal(HttpStatusCode.Unauthorized, (await otherDevice.GetAsync("/api/user/me")).StatusCode);
        }

        [Fact]
        public async Task Changing_a_name_or_a_password_requires_a_session()
        {
            var anonymous = factory.CreateHttpsClient();

            var name = await anonymous.PutAsJsonAsync("/api/user/me", new { firstName = "Sara", lastName = "Student" });
            var password = await ChangePassword(anonymous, ApiFactory.Password, "New-password-1");

            Assert.Equal(HttpStatusCode.Unauthorized, name.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, password.StatusCode);
        }
    }
}
