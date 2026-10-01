using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Prometej_tests
{
    public class QuizGameTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        private const string Correct = "A";
        private const string Wrong = "B";

        private record CreatedQuiz(int Id, int[] QuestionIds);

        // Every question has the options A to D and the correct answer A.
        private static async Task<CreatedQuiz> CreateQuiz(HttpClient client, int questionCount = 3)
        {
            var body = new
            {
                quiz = new { title = $"Kviz {Guid.NewGuid():N}", isPrivate = false, entryCode = 0 },
                questions = Enumerable.Range(1, questionCount).Select(i => new
                {
                    questionTitle = $"Pitanje {i}",
                    firstAnswer = "A",
                    secondAnswer = "B",
                    thirdAnswer = "C",
                    fourthAnswer = "D",
                    correctAnswer = Correct,
                    hintText = "",
                    exploreMore = "",
                }),
            };

            var response = await client.PostAsJsonAsync("/api/quiz/create", body);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var id = await response.Content.ReadFromJsonAsync<int>();
            var stored = await client.GetFromJsonAsync<JsonElement>($"/api/quiz/get/{id}");
            return new CreatedQuiz(id, stored.GetProperty("questions").EnumerateArray().Select(q => q.GetProperty("id").GetInt32()).ToArray());
        }

        private static object Answer(int questionId, string answerText = Correct) => new { questionId, answerText };

        private static object Submission(CreatedQuiz quiz, params string[] answerTexts) => new
        {
            quizId = quiz.Id,
            answers = quiz.QuestionIds.Select((id, i) => Answer(id, i < answerTexts.Length ? answerTexts[i] : Correct)),
        };

        private static Task<HttpResponseMessage> Submit(HttpClient client, object body) =>
            client.PostAsJsonAsync("/api/quiz/submit", body);

        private static async Task<JsonElement[]> Analytics(HttpClient client, int quizId) =>
            (await client.GetFromJsonAsync<JsonElement>($"/api/quiz/getAnalytics/{quizId}")).EnumerateArray().ToArray();

        private static async Task<int> GetOwnId(HttpClient client) =>
            (await client.GetFromJsonAsync<JsonElement>("/api/user/me")).GetProperty("id").GetInt32();

        [Fact]
        public async Task Submitting_requires_a_session()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var quiz = await CreateQuiz(teacher);

            var response = await Submit(factory.CreateHttpsClient(), Submission(quiz));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Empty(await Analytics(teacher, quiz.Id));
        }

        [Fact]
        public async Task A_play_is_recorded_for_the_signed_in_student_with_the_score_the_server_computed()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var quiz = await CreateQuiz(teacher);

            var response = await Submit(student, Submission(quiz));

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var game = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(3, game.GetProperty("score").GetInt32());
            Assert.Equal("Sara Student", game.GetProperty("userName").GetString());
            Assert.Equal(await GetOwnId(student), game.GetProperty("userId").GetInt32());
            Assert.Equal(3, game.GetProperty("answers").GetArrayLength());
        }

        [Fact]
        public async Task A_score_a_player_a_time_and_a_correct_answer_in_the_body_are_ignored()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var quiz = await CreateQuiz(teacher);
            var forged = new
            {
                quizId = quiz.Id,
                score = 999,
                userId = await GetOwnId(teacher),
                userName = "Netko Drugi",
                datePlayed = "2000-01-01T00:00:00Z",
                answers = quiz.QuestionIds.Select(id => new { questionId = id, answerText = Wrong, correctAnswer = Wrong }),
            };
            var before = DateTime.UtcNow;

            var response = await Submit(student, forged);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var stored = Assert.Single(await Analytics(teacher, quiz.Id));
            Assert.Equal(0, stored.GetProperty("score").GetInt32());
            Assert.Equal(await GetOwnId(student), stored.GetProperty("userId").GetInt32());
            Assert.Equal("Sara Student", stored.GetProperty("userName").GetString());
            // The container and the test share one clock; a minute allows for rounding.
            Assert.InRange(stored.GetProperty("datePlayed").GetDateTime().ToUniversalTime(), before.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1));
            Assert.All(stored.GetProperty("answers").EnumerateArray(),
                answer => Assert.Equal(Correct, answer.GetProperty("correctAnswer").GetString()));
        }

        [Fact]
        public async Task A_wrong_answer_lowers_the_score_and_is_stored_next_to_the_correct_one()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var quiz = await CreateQuiz(teacher);

            var response = await Submit(student, Submission(quiz, Wrong));

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var game = Assert.Single(await Analytics(teacher, quiz.Id));
            Assert.Equal(2, game.GetProperty("score").GetInt32());
            var wrong = game.GetProperty("answers").EnumerateArray().Single(a => a.GetProperty("questionId").GetInt32() == quiz.QuestionIds[0]);
            Assert.Equal(Wrong, wrong.GetProperty("answerText").GetString());
            Assert.Equal(Correct, wrong.GetProperty("correctAnswer").GetString());
        }

        [Fact]
        public async Task A_submission_that_does_not_match_the_quizs_questions_is_rejected_and_stores_nothing()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var quiz = await CreateQuiz(teacher);
            var otherQuiz = await CreateQuiz(teacher);
            var ids = quiz.QuestionIds;

            object Body(params object[] answers) => new { quizId = quiz.Id, answers };
            var missing = Body(Answer(ids[0]), Answer(ids[1]));
            var duplicated = Body(Answer(ids[0]), Answer(ids[1]), Answer(ids[1]));
            var extra = Body(Answer(ids[0]), Answer(ids[1]), Answer(ids[2]), Answer(otherQuiz.QuestionIds[0]));
            var fromAnotherQuiz = Body(Answer(ids[0]), Answer(ids[1]), Answer(otherQuiz.QuestionIds[0]));
            var notAnOption = Body(Answer(ids[0]), Answer(ids[1]), Answer(ids[2], "E"));
            var empty = Body();

            // Each body is paired with the reason the server gives, so a rejection for the wrong rule fails.
            const string everyQuestionOnce = "every question of the quiz exactly once";
            var cases = new (object Body, string Reason)[]
            {
                (missing, everyQuestionOnce),
                (duplicated, everyQuestionOnce),
                (extra, everyQuestionOnce),
                (fromAnotherQuiz, everyQuestionOnce),
                (notAnOption, "one of its question's options"),
                (empty, "Answers"),
            };
            foreach (var (body, reason) in cases)
            {
                var response = await Submit(student, body);

                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                Assert.Contains(reason, await response.Content.ReadAsStringAsync());
            }
            Assert.Empty(await Analytics(teacher, quiz.Id));
        }

        [Fact]
        public async Task A_quiz_without_questions_cannot_be_submitted()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var quiz = await CreateQuiz(teacher, questionCount: 0);

            var response = await Submit(student, Submission(quiz));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Empty(await Analytics(teacher, quiz.Id));
        }

        [Fact]
        public async Task Submitting_to_an_unknown_quiz_is_not_found()
        {
            var student = await factory.LoginAsNewStudent();

            var response = await Submit(student, new { quizId = 999_999, answers = new[] { Answer(1) } });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Each_play_is_its_own_quiz_game_and_analytics_lists_the_newest_first()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var quiz = await CreateQuiz(teacher);

            var first = await (await Submit(student, Submission(quiz, Wrong, Wrong, Wrong))).Content.ReadFromJsonAsync<JsonElement>();
            var second = await (await Submit(student, Submission(quiz))).Content.ReadFromJsonAsync<JsonElement>();

            var games = await Analytics(teacher, quiz.Id);
            Assert.Equal([second.GetProperty("id").GetInt32(), first.GetProperty("id").GetInt32()],
                games.Select(g => g.GetProperty("id").GetInt32()));
            Assert.Equal([3, 0], games.Select(g => g.GetProperty("score").GetInt32()));
            Assert.All(games, g => Assert.Equal(3, g.GetProperty("answers").GetArrayLength()));
        }

        [Fact]
        public async Task A_creators_play_of_their_own_quiz_is_not_recorded()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var admin = await factory.LoginAs(ApiFactory.AdminEmail);
            var quiz = await CreateQuiz(teacher);

            var byCreator = await Submit(teacher, Submission(quiz));

            Assert.Equal(HttpStatusCode.Forbidden, byCreator.StatusCode);
            Assert.Empty(await Analytics(teacher, quiz.Id));

            // An admin is not the creator here, so their play counts like anyone's.
            var byAdmin = await Submit(admin, Submission(quiz));

            Assert.Equal(HttpStatusCode.Created, byAdmin.StatusCode);
            Assert.Single(await Analytics(teacher, quiz.Id));
        }

        [Fact]
        public async Task Analytics_is_for_the_quizs_creator_and_admins()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var quiz = await CreateQuiz(teacher);
            var url = $"/api/quiz/getAnalytics/{quiz.Id}";

            async Task<HttpStatusCode> Get(HttpClient client) => (await client.GetAsync(url)).StatusCode;

            Assert.Equal(HttpStatusCode.OK, await Get(teacher));
            Assert.Equal(HttpStatusCode.OK, await Get(await factory.LoginAs(ApiFactory.AdminEmail)));
            Assert.Equal(HttpStatusCode.Forbidden, await Get(await factory.LoginAs(ApiFactory.OtherTeacherEmail)));
            Assert.Equal(HttpStatusCode.Forbidden, await Get(await factory.LoginAsNewStudent()));
            Assert.Equal(HttpStatusCode.Unauthorized, await Get(factory.CreateHttpsClient()));
            Assert.Equal(HttpStatusCode.NotFound, (await teacher.GetAsync("/api/quiz/getAnalytics/999999")).StatusCode);
        }

        [Fact]
        public async Task A_player_who_deletes_their_account_takes_their_quiz_games_along()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var leaving = await factory.LoginAsNewStudent();
            var staying = await factory.LoginAsNewStudent();
            var quiz = await CreateQuiz(teacher);
            await Submit(leaving, Submission(quiz));
            await Submit(staying, Submission(quiz));

            var delete = await leaving.DeleteAsync("/api/user/me");

            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
            var remaining = Assert.Single(await Analytics(teacher, quiz.Id));
            Assert.Equal(await GetOwnId(staying), remaining.GetProperty("userId").GetInt32());
        }

        [Fact]
        public async Task A_quiz_that_has_been_played_can_still_be_deleted()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var quiz = await CreateQuiz(teacher);
            await Submit(student, Submission(quiz));

            var delete = await teacher.DeleteAsync($"/api/quiz/delete/{quiz.Id}");

            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await teacher.GetAsync($"/api/quiz/getAnalytics/{quiz.Id}")).StatusCode);
        }
    }
}
