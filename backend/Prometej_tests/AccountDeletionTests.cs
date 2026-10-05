using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Prometej_tests
{
    // A class of its own: it deletes seeded accounts, which the other classes sign in with.
    public class AccountDeletionTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
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

        private static async Task<JsonElement[]> OwnQuizzes(HttpClient client)
        {
            var id = (await client.GetFromJsonAsync<JsonElement>("/api/user/me")).GetProperty("id").GetInt32();
            return (await client.GetFromJsonAsync<JsonElement>($"/api/quiz/getAllUserQuizzes/{id}")).EnumerateArray().ToArray();
        }

        [Fact]
        public async Task A_teacher_with_a_quiz_cannot_delete_the_account_until_the_quiz_is_deleted()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var quizId = await CreateQuiz(teacher);
            await Play(student, quizId);

            var refused = await teacher.DeleteAsync("/api/user/me");

            Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
            // Still signed in, and nothing was deleted along the way.
            Assert.Equal(HttpStatusCode.OK, (await teacher.GetAsync("/api/user/me")).StatusCode);
            var analytics = await teacher.GetFromJsonAsync<JsonElement>($"/api/quiz/getAnalytics/{quizId}");
            Assert.Equal(1, analytics.GetProperty("games").GetArrayLength());

            await teacher.DeleteAsync($"/api/quiz/delete/{quizId}");
            var allowed = await teacher.DeleteAsync("/api/user/me");

            Assert.Equal(HttpStatusCode.NoContent, allowed.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await teacher.GetAsync("/api/user/me")).StatusCode);
        }

        [Fact]
        public async Task An_admin_with_a_quiz_cannot_delete_the_account_either()
        {
            var admin = await factory.LoginAs(ApiFactory.AdminEmail);
            var quizId = await CreateQuiz(admin);

            var response = await admin.DeleteAsync("/api/user/me");

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/api/quiz/get/{quizId}")).StatusCode);
        }

        [Fact]
        public async Task A_student_who_played_can_delete_the_account()
        {
            var teacher = await factory.LoginAs(ApiFactory.OtherTeacherEmail);
            var student = await factory.LoginAsNewStudent();
            await Play(student, await CreateQuiz(teacher));

            var response = await student.DeleteAsync("/api/user/me");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task A_creators_quiz_list_counts_the_stored_plays_of_each_quiz()
        {
            var teacher = await factory.LoginAs(ApiFactory.OtherTeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var played = await CreateQuiz(teacher);
            var unplayed = await CreateQuiz(teacher);

            await Play(student, played);
            await Play(student, played);

            var counts = (await OwnQuizzes(teacher)).ToDictionary(
                q => q.GetProperty("id").GetInt32(), q => q.GetProperty("quizGameCount").GetInt32());
            Assert.Equal(2, counts[played]);
            Assert.Equal(0, counts[unplayed]);
        }
    }
}
