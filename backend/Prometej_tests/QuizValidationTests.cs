using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Prometej_tests
{
    public class QuizValidationTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        private static object Question(string title = "Pitanje", string first = "A", string second = "B",
            string third = "C", string fourth = "D", int correctOption = 1, int id = 0,
            string? hint = "", string? exploreMore = "") => new
        {
            id,
            questionTitle = title,
            firstAnswer = first,
            secondAnswer = second,
            thirdAnswer = third,
            fourthAnswer = fourth,
            correctOption,
            hintText = hint,
            exploreMore,
        };

        // Every way a quiz can break the rule "a quiz is valid or it is not saved".
        private static readonly (string Name, string Title, object[] Questions)[] InvalidQuizzes =
        [
            ("an empty title", "", [Question()]),
            ("a title of spaces", "   ", [Question()]),
            ("a title of 101 characters", new string('a', 101), [Question()]),
            ("no questions", "Kviz", []),
            ("an empty question title", "Kviz", [Question(title: "")]),
            ("an empty option", "Kviz", [Question(third: "")]),
            ("an option of spaces", "Kviz", [Question(third: "  ")]),
            ("two identical options", "Kviz", [Question(second: "A")]),
            ("two options that differ only by the spaces around them", "Kviz", [Question(second: " A ")]),
            ("correct option 0", "Kviz", [Question(correctOption: 0)]),
            ("correct option 5", "Kviz", [Question(correctOption: 5)]),
            ("an option of 501 characters", "Kviz", [Question(fourth: new string('a', 501))]),
            ("a question title of 501 characters", "Kviz", [Question(title: new string('a', 501))]),
            ("a hint of 1001 characters", "Kviz", [Question(hint: new string('a', 1001))]),
            ("further reading of 1001 characters", "Kviz", [Question(exploreMore: new string('a', 1001))]),
            ("an invalid question after a valid one", "Kviz", [Question(), Question(second: "A")]),
            ("a null in place of a question", "Kviz", [Question(), null!]),
        ];

        private static async Task<int> CreateQuiz(HttpClient client, bool isPrivate = false, string title = "Kviz")
        {
            var body = new { quiz = new { title, isPrivate }, questions = new[] { Question() } };
            var response = await client.PostAsJsonAsync("/api/quiz/create", body);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return await response.Content.ReadFromJsonAsync<int>();
        }

        private static Task<HttpResponseMessage> Update(HttpClient client, int id, string title, bool isPrivate, object[]? questions = null) =>
            client.PutAsJsonAsync("/api/quiz/update", new { quiz = new { id, title, isPrivate }, questions });

        private static async Task<JsonElement> GetQuiz(HttpClient client, int id) =>
            await client.GetFromJsonAsync<JsonElement>($"/api/quiz/get/{id}");

        private static async Task<int?> GetEntryCode(HttpClient client, int id)
        {
            var code = (await GetQuiz(client, id)).GetProperty("entryCode");
            return code.ValueKind == JsonValueKind.Null ? null : code.GetInt32();
        }

        private static async Task<int> GetOwnId(HttpClient client) =>
            (await client.GetFromJsonAsync<JsonElement>("/api/user/me")).GetProperty("id").GetInt32();

        private static async Task<int> CountOwnQuizzes(HttpClient client) =>
            (await client.GetFromJsonAsync<JsonElement>($"/api/quiz/getAllUserQuizzes/{await GetOwnId(client)}")).GetArrayLength();

        [Fact]
        public async Task An_invalid_quiz_is_not_created()
        {
            var teacher = await factory.LoginAs(ApiFactory.OtherTeacherEmail);
            var before = await CountOwnQuizzes(teacher);

            foreach (var (name, title, questions) in InvalidQuizzes)
            {
                var response = await teacher.PostAsJsonAsync("/api/quiz/create",
                    new { quiz = new { title, isPrivate = false }, questions });

                Assert.True(response.StatusCode == HttpStatusCode.BadRequest, $"{name}: {response.StatusCode}");
            }

            Assert.Equal(before, await CountOwnQuizzes(teacher));
        }

        [Fact]
        public async Task An_invalid_update_changes_nothing()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var id = await CreateQuiz(teacher);
            var before = (await GetQuiz(teacher, id)).GetRawText();

            foreach (var (name, title, questions) in InvalidQuizzes)
            {
                var response = await Update(teacher, id, title, isPrivate: false, questions);

                Assert.True(response.StatusCode == HttpStatusCode.BadRequest, $"{name}: {response.StatusCode}");
            }

            Assert.Equal(before, (await GetQuiz(teacher, id)).GetRawText());
        }

        [Fact]
        public async Task The_longest_allowed_texts_are_accepted_and_spaces_around_a_text_are_not_stored()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var longest = new string('a', 100);
            var body = new
            {
                quiz = new { title = longest, isPrivate = false },
                questions = new[] { Question(title: " Tko je napisao Juditu ", first: " Marko Marulić ", fourth: new string('d', 500)) },
            };

            var response = await teacher.PostAsJsonAsync("/api/quiz/create", body);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var stored = await GetQuiz(teacher, await response.Content.ReadFromJsonAsync<int>());
            Assert.Equal(longest, stored.GetProperty("title").GetString());
            var question = stored.GetProperty("questions")[0];
            Assert.Equal("Tko je napisao Juditu", question.GetProperty("questionTitle").GetString());
            Assert.Equal("Marko Marulić", question.GetProperty("firstAnswer").GetString());
            Assert.Equal(500, question.GetProperty("fourthAnswer").GetString()!.Length);
        }

        [Fact]
        public async Task The_correct_option_is_stored_as_given_and_survives_an_edit_of_its_text()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var id = await CreateQuiz(teacher);
            var questionId = (await GetQuiz(teacher, id)).GetProperty("questions")[0].GetProperty("id").GetInt32();

            var response = await Update(teacher, id, " Kviz o Juditi ", isPrivate: false,
                [Question(third: "Novi tekst", correctOption: 3, id: questionId)]);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            var stored = await GetQuiz(teacher, id);
            Assert.Equal("Kviz o Juditi", stored.GetProperty("title").GetString());
            var question = stored.GetProperty("questions")[0];
            Assert.Equal(3, question.GetProperty("correctOption").GetInt32());
            Assert.Equal("Novi tekst", question.GetProperty("thirdAnswer").GetString());
        }

        [Fact]
        public async Task A_hint_and_further_reading_are_stored_trimmed_and_an_empty_one_is_stored_as_none()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var body = new
            {
                quiz = new { title = "Kviz", isPrivate = false },
                questions = new[] { Question(hint: " Sjetite se Marulića. ", exploreMore: "   ") },
            };

            var id = await (await teacher.PostAsJsonAsync("/api/quiz/create", body)).Content.ReadFromJsonAsync<int>();

            var created = (await GetQuiz(teacher, id)).GetProperty("questions")[0];
            Assert.Equal("Sjetite se Marulića.", created.GetProperty("hintText").GetString());
            Assert.Equal(JsonValueKind.Null, created.GetProperty("exploreMore").ValueKind);

            var questionId = created.GetProperty("id").GetInt32();
            var cleared = await Update(teacher, id, "Kviz", isPrivate: false,
                [Question(id: questionId, hint: "", exploreMore: new string('a', 1000))]);

            Assert.Equal(HttpStatusCode.NoContent, cleared.StatusCode);
            var updated = (await GetQuiz(teacher, id)).GetProperty("questions")[0];
            Assert.Equal(JsonValueKind.Null, updated.GetProperty("hintText").ValueKind);
            Assert.Equal(1000, updated.GetProperty("exploreMore").GetString()!.Length);

            var withNull = await Update(teacher, id, "Kviz", isPrivate: false,
                [Question(id: questionId, hint: null, exploreMore: null)]);

            Assert.Equal(HttpStatusCode.NoContent, withNull.StatusCode);
            var nulled = (await GetQuiz(teacher, id)).GetProperty("questions")[0];
            Assert.Equal(JsonValueKind.Null, nulled.GetProperty("hintText").ValueKind);
            Assert.Equal(JsonValueKind.Null, nulled.GetProperty("exploreMore").ValueKind);
        }

        [Fact]
        public async Task A_quiz_stored_without_questions_can_still_be_renamed()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var id = factory.AddLegacyQuiz(await GetOwnId(teacher));

            var response = await Update(teacher, id, "Novo ime", isPrivate: false);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal("Novo ime", (await GetQuiz(teacher, id)).GetProperty("title").GetString());
        }

        [Fact]
        public async Task The_server_makes_the_entry_code_and_only_a_private_quiz_has_one()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            object Body(bool isPrivate) => new
            {
                quiz = new { title = "Kviz", isPrivate, entryCode = 12 },
                questions = new[] { Question() },
            };

            var privateId = await (await teacher.PostAsJsonAsync("/api/quiz/create", Body(true))).Content.ReadFromJsonAsync<int>();
            var publicId = await (await teacher.PostAsJsonAsync("/api/quiz/create", Body(false))).Content.ReadFromJsonAsync<int>();

            // The code in the body is ignored both times.
            Assert.InRange((await GetEntryCode(teacher, privateId))!.Value, 10000, 99999);
            Assert.Null(await GetEntryCode(teacher, publicId));
        }

        [Fact]
        public async Task The_entry_code_goes_when_a_quiz_is_made_public_and_a_new_one_comes_when_it_is_private_again()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var id = await CreateQuiz(teacher, isPrivate: true);
            var code = await GetEntryCode(teacher, id);

            await Update(teacher, id, "Preimenovan", isPrivate: true);
            var afterRename = await GetEntryCode(teacher, id);
            await Update(teacher, id, "Preimenovan", isPrivate: false);
            var whilePublic = await GetEntryCode(teacher, id);
            var oldCode = await factory.CreateHttpsClient().GetAsync($"/api/quiz/getByCode/{code}");
            await Update(teacher, id, "Preimenovan", isPrivate: true);
            var privateAgain = await GetEntryCode(teacher, id);

            Assert.Equal(code, afterRename);
            Assert.Null(whilePublic);
            Assert.Equal(HttpStatusCode.NotFound, oldCode.StatusCode);
            Assert.InRange(privateAgain!.Value, 10000, 99999);
        }

        [Fact]
        public async Task Private_quizzes_never_share_an_entry_code()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var codes = new List<int?>();

            for (var i = 0; i < 30; i++)
            {
                codes.Add(await GetEntryCode(teacher, await CreateQuiz(teacher, isPrivate: true)));
            }

            Assert.Equal(30, codes.Distinct().Count());
        }

        [Fact]
        public async Task An_entry_code_left_on_a_public_quiz_does_not_open_it()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            factory.AddLegacyQuiz(await GetOwnId(teacher), isPrivate: false, entryCode: 123456);

            var response = await factory.CreateHttpsClient().GetAsync("/api/quiz/getByCode/123456");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
