using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Prometej_tests
{
    // The tests of a class share one database, so a test that reads the public list gives its
    // titles a word no other test uses and searches for it.
    public class QuizPeriodTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        private const int Renesansa = 3;
        private const int Realizam = 4;
        private const int Romantizam = 5;
        private const int Modernizam = 6;

        private static string Word() => $"w{Guid.NewGuid():N}"[..12];

        private static object Question(string title = "Pitanje", int id = 0) => new
        {
            id,
            questionTitle = title,
            firstAnswer = "A",
            secondAnswer = "B",
            thirdAnswer = "C",
            fourthAnswer = "D",
            correctOption = 1,
        };

        private static Task<HttpResponseMessage> Create(HttpClient client, string title, int? periodId, bool isPrivate = false, int questionCount = 1) =>
            client.PostAsJsonAsync("/api/quiz/create", new
            {
                quiz = new { title, isPrivate, periodId },
                questions = Enumerable.Range(1, questionCount).Select(i => Question($"Pitanje {i}")),
            });

        private static async Task<int> CreateQuiz(HttpClient client, string title, int? periodId, bool isPrivate = false, int questionCount = 1)
        {
            var response = await Create(client, title, periodId, isPrivate, questionCount);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return await response.Content.ReadFromJsonAsync<int>();
        }

        private static async Task<JsonElement> GetQuiz(HttpClient client, int id) =>
            await client.GetFromJsonAsync<JsonElement>($"/api/quiz/get/{id}");

        private static int? PeriodIdOf(JsonElement quiz) =>
            quiz.GetProperty("periodId").ValueKind == JsonValueKind.Null ? null : quiz.GetProperty("periodId").GetInt32();

        private async Task<JsonElement[]> Search(string parameters)
        {
            var response = await factory.CreateHttpsClient().GetAsync($"/api/quiz/search?{parameters}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().ToArray();
        }

        private async Task<string[]> Titles(string parameters) =>
            (await Search(parameters)).Select(quiz => quiz.GetProperty("title").GetString()!).ToArray();

        private static async Task<int> GetOwnId(HttpClient client) =>
            (await client.GetFromJsonAsync<JsonElement>("/api/user/me")).GetProperty("id").GetInt32();

        private static async Task<JsonElement[]> OwnQuizzes(HttpClient client) =>
            (await client.GetFromJsonAsync<JsonElement>("/api/quiz/getMyQuizzes")).EnumerateArray().ToArray();

        private async Task<int> QuizCountOf(int periodId) =>
            (await factory.CreateHttpsClient().GetFromJsonAsync<JsonElement>("/api/period")).EnumerateArray()
                .Single(period => period.GetProperty("id").GetInt32() == periodId).GetProperty("quizCount").GetInt32();

        [Fact]
        public async Task A_quiz_keeps_the_period_it_was_created_with()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);

            var withPeriod = await GetQuiz(teacher, await CreateQuiz(teacher, "S razdobljem", Realizam));
            var without = await GetQuiz(teacher, await CreateQuiz(teacher, "Bez razdoblja", null));
            var notSent = await teacher.PostAsJsonAsync("/api/quiz/create",
                new { quiz = new { title = "Bez polja", isPrivate = false }, questions = new[] { Question() } });

            Assert.Equal(Realizam, PeriodIdOf(withPeriod));
            Assert.Equal("Realizam", withPeriod.GetProperty("periodName").GetString());
            Assert.Null(PeriodIdOf(without));
            Assert.Equal(JsonValueKind.Null, without.GetProperty("periodName").ValueKind);
            Assert.Equal(HttpStatusCode.Created, notSent.StatusCode);
            Assert.Null(PeriodIdOf(await GetQuiz(teacher, await notSent.Content.ReadFromJsonAsync<int>())));
        }

        [Fact]
        public async Task A_period_that_does_not_exist_is_refused()
        {
            var teacher = await factory.LoginAs(ApiFactory.OtherTeacherEmail);
            var id = await CreateQuiz(teacher, "Kviz", Realizam);
            var countBefore = (await OwnQuizzes(teacher)).Length;
            var quizBefore = (await GetQuiz(teacher, id)).GetRawText();

            foreach (var periodId in new[] { 99, 0, -1 })
            {
                var created = await Create(teacher, "Kviz", periodId);
                var updated = await teacher.PutAsJsonAsync("/api/quiz/update",
                    new { quiz = new { id, title = "Drugi naslov", isPrivate = false, periodId } });

                Assert.Equal(HttpStatusCode.BadRequest, created.StatusCode);
                Assert.Equal(HttpStatusCode.BadRequest, updated.StatusCode);
            }

            Assert.Equal(countBefore, (await OwnQuizzes(teacher)).Length);
            Assert.Equal(quizBefore, (await GetQuiz(teacher, id)).GetRawText());
        }

        [Fact]
        public async Task An_update_sets_the_period_and_one_without_it_removes_it()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var id = await CreateQuiz(teacher, "Kviz", Realizam, questionCount: 2);
            var questionsBefore = (await GetQuiz(teacher, id)).GetProperty("questions").GetRawText();

            var moved = await teacher.PutAsJsonAsync("/api/quiz/update",
                new { quiz = new { id, title = "Kviz", isPrivate = false, periodId = Modernizam } });
            var afterMove = await GetQuiz(teacher, id);
            // The header is sent whole: a request that leaves the Period out says the Quiz has none.
            var headerOnly = await teacher.PutAsJsonAsync("/api/quiz/update",
                new { quiz = new { id, title = "Kviz", isPrivate = false } });
            var afterHeaderOnly = await GetQuiz(teacher, id);

            Assert.Equal(HttpStatusCode.NoContent, moved.StatusCode);
            Assert.Equal(Modernizam, PeriodIdOf(afterMove));
            Assert.Equal("Modernizam", afterMove.GetProperty("periodName").GetString());
            Assert.Equal(questionsBefore, afterMove.GetProperty("questions").GetRawText());
            Assert.Equal(HttpStatusCode.NoContent, headerOnly.StatusCode);
            Assert.Null(PeriodIdOf(afterHeaderOnly));
            Assert.Equal(questionsBefore, afterHeaderOnly.GetProperty("questions").GetRawText());

            // An explicit null says the same.
            await teacher.PutAsJsonAsync("/api/quiz/update", new { quiz = new { id, title = "Kviz", isPrivate = false, periodId = Realizam } });
            var cleared = await teacher.PutAsJsonAsync("/api/quiz/update",
                new { quiz = new { id, title = "Kviz", isPrivate = false, periodId = (int?)null } });
            Assert.Equal(HttpStatusCode.NoContent, cleared.StatusCode);
            Assert.Null(PeriodIdOf(await GetQuiz(teacher, id)));
        }

        [Fact]
        public async Task Another_teacher_learns_nothing_about_periods()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var other = await factory.LoginAs(ApiFactory.OtherTeacherEmail);
            var id = await CreateQuiz(teacher, "Kviz", Realizam);

            var response = await other.PutAsJsonAsync("/api/quiz/update",
                new { quiz = new { id, title = "Kviz", isPrivate = false, periodId = 99 } });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task The_list_names_each_quizs_period_and_counts_its_questions()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var word = Word();
            var id = await CreateQuiz(teacher, $"Kviz {word}", Realizam, questionCount: 2);

            var listed = Assert.Single(await Search($"query={word}"));
            var own = (await OwnQuizzes(teacher)).Single(quiz => quiz.GetProperty("id").GetInt32() == id);

            foreach (var quiz in new[] { listed, own })
            {
                Assert.Equal(Realizam, PeriodIdOf(quiz));
                Assert.Equal("Realizam", quiz.GetProperty("periodName").GetString());
                Assert.Equal(2, quiz.GetProperty("questionCount").GetInt32());
            }
        }

        [Fact]
        public async Task Search_can_be_narrowed_to_one_period()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var word = Word();
            await CreateQuiz(teacher, $"Renesansa {word}", Renesansa);
            await CreateQuiz(teacher, $"Realizam {word}", Realizam);
            await CreateQuiz(teacher, $"Bez razdoblja {word}", null);
            await CreateQuiz(teacher, $"Privatni {word}", Realizam, isPrivate: true);

            var ofThePeriod = await Titles($"periodId={Realizam}");

            Assert.Equal([$"Realizam {word}"], await Titles($"query={word}&periodId={Realizam}"));
            Assert.Contains($"Realizam {word}", ofThePeriod);
            Assert.DoesNotContain($"Renesansa {word}", ofThePeriod);
            Assert.DoesNotContain($"Bez razdoblja {word}", ofThePeriod);
            Assert.DoesNotContain($"Privatni {word}", ofThePeriod);
            Assert.Empty(await Titles($"query={word}&periodId=99"));
            Assert.Equal(3, (await Titles($"query={word}")).Length);
        }

        [Fact]
        public async Task Quizzes_come_in_curriculum_order_and_those_without_a_period_last()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var word = Word();
            await CreateQuiz(teacher, $"Bez razdoblja {word}", null);
            await CreateQuiz(teacher, $"Realizam {word}", Realizam);
            await CreateQuiz(teacher, $"Romantizam {word}", Romantizam);
            await CreateQuiz(teacher, $"Antika {word}", 1);
            await CreateQuiz(teacher, $"Realizam drugi {word}", Realizam);

            // Romantizam comes before Realizam, though its id is the higher one.
            Assert.Equal(
                [$"Antika {word}", $"Romantizam {word}", $"Realizam {word}", $"Realizam drugi {word}", $"Bez razdoblja {word}"],
                await Titles($"query={word}"));
        }

        [Fact]
        public async Task A_retired_question_is_not_counted()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var word = Word();
            var id = await CreateQuiz(teacher, $"Kviz {word}", Realizam, questionCount: 2);
            var questionIds = (await GetQuiz(teacher, id)).GetProperty("questions").EnumerateArray()
                .Select(question => question.GetProperty("id").GetInt32()).ToArray();
            var play = await student.PostAsJsonAsync("/api/quiz/submit",
                new { quizId = id, answers = questionIds.Select(questionId => new { questionId, chosenOption = 1 }) });
            play.EnsureSuccessStatusCode();

            // The played Question left out here is retired, not deleted.
            var update = await teacher.PutAsJsonAsync("/api/quiz/update", new
            {
                quiz = new { id, title = $"Kviz {word}", isPrivate = false, periodId = Realizam },
                questions = new[] { Question("Pitanje 1", questionIds[0]) },
            });

            Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);
            Assert.Equal(1, Assert.Single(await Search($"query={word}")).GetProperty("questionCount").GetInt32());
            Assert.Equal(1, (await OwnQuizzes(teacher)).Single(quiz => quiz.GetProperty("id").GetInt32() == id)
                .GetProperty("questionCount").GetInt32());
        }

        [Fact]
        public async Task A_quiz_without_questions_is_not_listed_but_its_creator_still_sees_it()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var id = factory.AddLegacyQuiz(await GetOwnId(teacher));
            var title = (await GetQuiz(teacher, id)).GetProperty("title").GetString()!;

            var own = (await OwnQuizzes(teacher)).Single(quiz => quiz.GetProperty("id").GetInt32() == id);

            Assert.DoesNotContain(title, await Titles(""));
            Assert.Empty(await Titles($"query={title}"));
            Assert.Equal(0, own.GetProperty("questionCount").GetInt32());
            Assert.Equal(HttpStatusCode.OK, (await factory.CreateHttpsClient().GetAsync($"/api/quiz/get/{id}")).StatusCode);
        }

        // No other test of the class uses Period 11.
        [Fact]
        public async Task A_period_counts_its_listed_quizzes()
        {
            const int ekspresionizam = 11;
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var before = await QuizCountOf(ekspresionizam);

            await CreateQuiz(teacher, "Javni", ekspresionizam);
            await CreateQuiz(teacher, "Privatni", ekspresionizam, isPrivate: true);
            var legacyId = factory.AddLegacyQuiz(await GetOwnId(teacher));
            var moved = await teacher.PutAsJsonAsync("/api/quiz/update",
                new { quiz = new { id = legacyId, title = "Stari kviz", isPrivate = false, periodId = ekspresionizam } });

            Assert.Equal(HttpStatusCode.NoContent, moved.StatusCode);
            Assert.Equal(ekspresionizam, PeriodIdOf(await GetQuiz(teacher, legacyId)));
            Assert.Equal(before + 1, await QuizCountOf(ekspresionizam));
        }
    }
}
