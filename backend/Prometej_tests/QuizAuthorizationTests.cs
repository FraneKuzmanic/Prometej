using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Prometej_tests
{
    public class QuizAuthorizationTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        private static object Question(string title, int id = 0) => new
        {
            id,
            questionTitle = title,
            firstAnswer = "A",
            secondAnswer = "B",
            thirdAnswer = "C",
            fourthAnswer = "D",
            correctOption = 1,
            hintText = "",
            exploreMore = "",
        };

        private record CreatedQuiz(int Id, string Title, int? EntryCode);

        private static async Task<CreatedQuiz> CreateQuiz(HttpClient client, bool isPrivate = false)
        {
            var title = $"Kviz {Guid.NewGuid():N}";
            var body = new
            {
                quiz = new { title, isPrivate },
                questions = new[] { Question("Prvo pitanje") },
            };

            var response = await client.PostAsJsonAsync("/api/quiz/create", body);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var id = await response.Content.ReadFromJsonAsync<int>();
            // The server makes the entry code; its creator reads it back.
            int? entryCode = isPrivate ? (await GetQuiz(client, id)).GetProperty("entryCode").GetInt32() : null;
            return new CreatedQuiz(id, title, entryCode);
        }

        private static object UpdateOf(CreatedQuiz quiz, string title, params object[] questions) => new
        {
            quiz = new { id = quiz.Id, title, isPrivate = quiz.EntryCode != null },
            // No list leaves the questions as they are; an empty one would be refused.
            questions = questions.Length > 0 ? questions : null,
        };

        private static async Task<JsonElement> GetQuiz(HttpClient client, int id) =>
            await client.GetFromJsonAsync<JsonElement>($"/api/quiz/get/{id}");

        private static async Task<int> GetOwnId(HttpClient client) =>
            (await client.GetFromJsonAsync<JsonElement>("/api/user/me")).GetProperty("id").GetInt32();

        [Fact]
        public async Task Public_quiz_list_is_open_to_anonymous_callers()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var quiz = await CreateQuiz(teacher);

            var response = await factory.CreateHttpsClient().GetAsync("/api/quiz/getAll");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains(quiz.Title, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Creating_a_quiz_requires_a_teacher_or_admin()
        {
            var body = new { quiz = new { title = "Kviz", isPrivate = false }, questions = new[] { Question("Pitanje") } };

            var anonymous = await factory.CreateHttpsClient().PostAsJsonAsync("/api/quiz/create", body);
            var student = await (await factory.LoginAsNewStudent()).PostAsJsonAsync("/api/quiz/create", body);

            Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, student.StatusCode);
        }

        [Fact]
        public async Task The_creator_is_the_caller_whatever_id_the_body_carries()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var otherTeacherId = await GetOwnId(await factory.LoginAs(ApiFactory.OtherTeacherEmail));
            var body = new
            {
                quiz = new { title = "Tuđi kviz", isPrivate = false, creatorId = otherTeacherId },
                questions = new[] { Question("Pitanje") },
            };

            var response = await teacher.PostAsJsonAsync("/api/quiz/create", body);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var stored = await GetQuiz(teacher, await response.Content.ReadFromJsonAsync<int>());
            Assert.Equal(await GetOwnId(teacher), stored.GetProperty("creatorId").GetInt32());
        }

        [Fact]
        public async Task Another_teacher_cannot_update_or_delete_a_quiz()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var otherTeacher = await factory.LoginAs(ApiFactory.OtherTeacherEmail);
            var quiz = await CreateQuiz(teacher);

            var update = await otherTeacher.PutAsJsonAsync("/api/quiz/update", UpdateOf(quiz, "Preoteto"));
            var delete = await otherTeacher.DeleteAsync($"/api/quiz/delete/{quiz.Id}");

            Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
            Assert.Equal(quiz.Title, (await GetQuiz(teacher, quiz.Id)).GetProperty("title").GetString());
        }

        [Fact]
        public async Task The_creator_can_update_a_quiz_and_its_questions()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var quiz = await CreateQuiz(teacher);
            var questionId = (await GetQuiz(teacher, quiz.Id)).GetProperty("questions")[0].GetProperty("id").GetInt32();

            var response = await teacher.PutAsJsonAsync("/api/quiz/update",
                UpdateOf(quiz, "Novi naslov", Question("Izmijenjeno pitanje", questionId), Question("Drugo pitanje")));

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            var stored = await GetQuiz(teacher, quiz.Id);
            Assert.Equal("Novi naslov", stored.GetProperty("title").GetString());
            var titles = stored.GetProperty("questions").EnumerateArray()
                .Select(q => q.GetProperty("questionTitle").GetString()).Order().ToArray();
            Assert.Equal(["Drugo pitanje", "Izmijenjeno pitanje"], titles);
        }

        [Fact]
        public async Task An_admin_can_update_and_delete_any_quiz()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var admin = await factory.LoginAs(ApiFactory.AdminEmail);
            var quiz = await CreateQuiz(teacher);

            var update = await admin.PutAsJsonAsync("/api/quiz/update", UpdateOf(quiz, "Uredio admin"));

            Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);
            var stored = await GetQuiz(teacher, quiz.Id);
            Assert.Equal("Uredio admin", stored.GetProperty("title").GetString());
            // The quiz stays with its creator when an admin edits it.
            Assert.Equal(await GetOwnId(teacher), stored.GetProperty("creatorId").GetInt32());

            var delete = await admin.DeleteAsync($"/api/quiz/delete/{quiz.Id}");

            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await teacher.GetAsync($"/api/quiz/get/{quiz.Id}")).StatusCode);
        }

        [Fact]
        public async Task An_update_cannot_reach_a_question_of_another_quiz()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var otherTeacher = await factory.LoginAs(ApiFactory.OtherTeacherEmail);
            var victimQuiz = await CreateQuiz(teacher);
            var victimQuestionId = (await GetQuiz(teacher, victimQuiz.Id)).GetProperty("questions")[0].GetProperty("id").GetInt32();
            var ownQuiz = await CreateQuiz(otherTeacher);

            var response = await otherTeacher.PutAsJsonAsync("/api/quiz/update",
                UpdateOf(ownQuiz, "Moj kviz", Question("Podmetnuto", victimQuestionId)));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            var victimQuestion = (await GetQuiz(teacher, victimQuiz.Id)).GetProperty("questions")[0];
            Assert.Equal("Prvo pitanje", victimQuestion.GetProperty("questionTitle").GetString());
            // The rejected request changed nothing, including its own quiz's title.
            Assert.Equal(ownQuiz.Title, (await GetQuiz(otherTeacher, ownQuiz.Id)).GetProperty("title").GetString());
        }

        [Fact]
        public async Task Updating_or_deleting_an_unknown_quiz_is_not_found()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var missing = new CreatedQuiz(999_999, "Nema ga", null);

            var update = await teacher.PutAsJsonAsync("/api/quiz/update", UpdateOf(missing, "Nema ga"));
            var delete = await teacher.DeleteAsync("/api/quiz/delete/999999");

            Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        }

        [Fact]
        public async Task Search_does_not_return_a_private_quiz()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var privateQuiz = await CreateQuiz(teacher, isPrivate: true);
            var publicQuiz = await CreateQuiz(teacher);
            var anonymous = factory.CreateHttpsClient();

            var privateHits = await anonymous.GetStringAsync($"/api/quiz/search?query={privateQuiz.Title}");
            var publicHits = await anonymous.GetStringAsync($"/api/quiz/search?query={publicQuiz.Title}");
            var byCreator = await anonymous.GetStringAsync("/api/quiz/search?query=Tea");

            Assert.Equal("[]", privateHits);
            Assert.Contains(publicQuiz.Title, publicHits);
            Assert.DoesNotContain(privateQuiz.Title, byCreator);
            Assert.DoesNotContain(privateQuiz.EntryCode.ToString()!, byCreator);
        }

        [Fact]
        public async Task A_private_quiz_opens_only_with_its_entry_code_or_for_its_creator()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var quiz = await CreateQuiz(teacher, isPrivate: true);
            var anonymous = factory.CreateHttpsClient();
            var otherTeacher = await factory.LoginAs(ApiFactory.OtherTeacherEmail);
            var admin = await factory.LoginAs(ApiFactory.AdminEmail);

            async Task<HttpStatusCode> Get(HttpClient client, string query = "") =>
                (await client.GetAsync($"/api/quiz/get/{quiz.Id}{query}")).StatusCode;

            Assert.Equal(HttpStatusCode.NotFound, await Get(anonymous));
            Assert.Equal(HttpStatusCode.NotFound, await Get(anonymous, $"?code={quiz.EntryCode + 1}"));
            Assert.Equal(HttpStatusCode.NotFound, await Get(otherTeacher));
            Assert.Equal(HttpStatusCode.OK, await Get(anonymous, $"?code={quiz.EntryCode}"));
            Assert.Equal(HttpStatusCode.OK, await Get(teacher));
            Assert.Equal(HttpStatusCode.OK, await Get(admin));
        }

        [Fact]
        public async Task The_entry_code_finds_its_quiz_and_an_unknown_code_is_not_found()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var quiz = await CreateQuiz(teacher, isPrivate: true);
            var anonymous = factory.CreateHttpsClient();

            var found = await anonymous.GetFromJsonAsync<JsonElement>($"/api/quiz/getByCode/{quiz.EntryCode}");
            // Below the five digits the server hands out, so no quiz can have it.
            var unknown = await anonymous.GetAsync("/api/quiz/getByCode/9999");

            Assert.Equal(quiz.Id, found.GetProperty("id").GetInt32());
            Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        }

        [Fact]
        public async Task A_teacher_lists_only_their_own_quizzes()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var otherTeacher = await factory.LoginAs(ApiFactory.OtherTeacherEmail);
            var admin = await factory.LoginAs(ApiFactory.AdminEmail);
            var student = await factory.LoginAsNewStudent();
            var quiz = await CreateQuiz(teacher, isPrivate: true);
            var url = $"/api/quiz/getAllUserQuizzes/{await GetOwnId(teacher)}";

            Assert.Contains(quiz.Title, await teacher.GetStringAsync(url));
            Assert.Contains(quiz.Title, await admin.GetStringAsync(url));
            Assert.Equal(HttpStatusCode.Forbidden, (await otherTeacher.GetAsync(url)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await student.GetAsync(url)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateHttpsClient().GetAsync(url)).StatusCode);
        }
    }
}
