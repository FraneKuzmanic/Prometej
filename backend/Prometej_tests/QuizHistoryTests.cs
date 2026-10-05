using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Prometej_tests
{
    // The tests of a class share one database, so each test signs in a Student of its own, and
    // a test that reads a Period's progress uses a Period no other test of the class does.
    public class QuizHistoryTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        private const int Realizam = 4;
        private const int Romantizam = 5;

        private record CreatedQuiz(int Id, int[] QuestionIds);

        // The options are the four letters, in order.
        private static object Question(string title, string exploreMore = "", int correctOption = 1, int id = 0, string options = "ABCD") => new
        {
            id,
            questionTitle = title,
            firstAnswer = options[..1],
            secondAnswer = options[1..2],
            thirdAnswer = options[2..3],
            fourthAnswer = options[3..],
            correctOption,
            exploreMore,
        };

        private static object[] Questions(int count) =>
            Enumerable.Range(1, count).Select(i => Question($"Pitanje {i}")).ToArray();

        private static async Task<CreatedQuiz> CreateQuiz(HttpClient client, object[] questions, int? periodId = null, bool isPrivate = false)
        {
            var response = await client.PostAsJsonAsync("/api/quiz/create",
                new { quiz = new { title = $"Kviz {Guid.NewGuid():N}", isPrivate, periodId }, questions });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var id = await response.Content.ReadFromJsonAsync<int>();
            var stored = await client.GetFromJsonAsync<JsonElement>($"/api/quiz/get/{id}");
            return new CreatedQuiz(id, stored.GetProperty("questions").EnumerateArray().Select(q => q.GetProperty("id").GetInt32()).ToArray());
        }

        private static async Task Update(HttpClient client, CreatedQuiz quiz, bool isPrivate = false, int? periodId = null, object[]? questions = null)
        {
            var response = await client.PutAsJsonAsync("/api/quiz/update",
                new { quiz = new { id = quiz.Id, title = "Kviz", isPrivate, periodId }, questions });

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        // Plays the quiz with the given options, in the order of its questions, and returns the id of the stored game.
        private static async Task<int> Play(HttpClient client, CreatedQuiz quiz, params int[] chosenOptions)
        {
            var response = await client.PostAsJsonAsync("/api/quiz/submit", new
            {
                quizId = quiz.Id,
                answers = quiz.QuestionIds.Select((questionId, i) => new { questionId, chosenOption = chosenOptions[i] }),
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        }

        private static Task<JsonElement> Review(HttpClient client, int gameId) =>
            client.GetFromJsonAsync<JsonElement>($"/api/quiz/getGame/{gameId}");

        private static async Task<JsonElement[]> Games(HttpClient client) =>
            (await client.GetFromJsonAsync<JsonElement>("/api/quiz/getMyGames")).GetProperty("games").EnumerateArray().ToArray();

        private static async Task<JsonElement[]> Progress(HttpClient client) =>
            (await client.GetFromJsonAsync<JsonElement>("/api/quiz/getMyGames")).GetProperty("progress").EnumerateArray().ToArray();

        private static void AssertProgress(JsonElement row, int quizCount, int playedCount, int averageBestPercent)
        {
            Assert.Equal(quizCount, row.GetProperty("quizCount").GetInt32());
            Assert.Equal(playedCount, row.GetProperty("playedCount").GetInt32());
            Assert.Equal(averageBestPercent, row.GetProperty("averageBestPercent").GetInt32());
        }

        [Fact]
        public async Task A_game_opens_for_its_player_and_nobody_else()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var quiz = await CreateQuiz(teacher, Questions(2));
            var gameId = await Play(student, quiz, 1, 2);
            var url = $"/api/quiz/getGame/{gameId}";

            async Task<HttpStatusCode> Get(HttpClient client) => (await client.GetAsync(url)).StatusCode;

            var review = await Review(student, gameId);
            Assert.StartsWith("Kviz ", review.GetProperty("quizTitle").GetString());
            Assert.Equal(1, review.GetProperty("score").GetInt32());
            Assert.True(review.GetProperty("quizIsListed").GetBoolean());
            Assert.Equal(HttpStatusCode.NotFound, await Get(await factory.LoginAsNewStudent()));
            Assert.Equal(HttpStatusCode.NotFound, await Get(teacher));
            Assert.Equal(HttpStatusCode.NotFound, await Get(await factory.LoginAs(ApiFactory.AdminEmail)));
            Assert.Equal(HttpStatusCode.Unauthorized, await Get(factory.CreateHttpsClient()));
            Assert.Equal(HttpStatusCode.NotFound, (await student.GetAsync("/api/quiz/getGame/999999")).StatusCode);
        }

        [Fact]
        public async Task A_review_shows_each_question_as_it_was_played()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var quiz = await CreateQuiz(teacher, [Question("Staro pitanje", exploreMore: "Staro objašnjenje")]);
            var firstId = await Play(student, quiz, 2);

            await Update(teacher, quiz, questions:
                [Question("Novo pitanje", exploreMore: "Novo objašnjenje", correctOption: 3, id: quiz.QuestionIds[0], options: "WXYZ")]);
            var secondId = await Play(student, quiz, 3);

            var first = (await Review(student, firstId)).GetProperty("answers")[0];
            Assert.Equal("Staro pitanje", first.GetProperty("questionTitle").GetString());
            Assert.Equal("Staro objašnjenje", first.GetProperty("exploreMore").GetString());
            Assert.Equal("B", first.GetProperty("answerText").GetString());
            Assert.Equal("A", first.GetProperty("correctAnswer").GetString());
            var second = (await Review(student, secondId)).GetProperty("answers")[0];
            Assert.Equal("Novo pitanje", second.GetProperty("questionTitle").GetString());
            Assert.Equal("Novo objašnjenje", second.GetProperty("exploreMore").GetString());
            Assert.Equal("Y", second.GetProperty("answerText").GetString());
            Assert.Equal("Y", second.GetProperty("correctAnswer").GetString());
        }

        [Fact]
        public async Task A_review_keeps_a_retired_question_and_lists_answers_in_question_order()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var quiz = await CreateQuiz(teacher, Questions(3));
            var submit = await student.PostAsJsonAsync("/api/quiz/submit", new
            {
                quizId = quiz.Id,
                answers = quiz.QuestionIds.Reverse().Select(questionId => new { questionId, chosenOption = 1 }),
            });
            var gameId = (await submit.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

            // The second Question was played, so leaving it out retires it.
            await Update(teacher, quiz, questions:
                [Question("Pitanje 1", id: quiz.QuestionIds[0]), Question("Pitanje 3", id: quiz.QuestionIds[2])]);

            var review = await Review(student, gameId);
            Assert.Equal(["Pitanje 1", "Pitanje 2", "Pitanje 3"],
                review.GetProperty("answers").EnumerateArray().Select(a => a.GetProperty("questionTitle").GetString()));
            Assert.Equal(3, review.GetProperty("score").GetInt32());
        }

        [Fact]
        public async Task A_game_of_a_quiz_made_private_can_still_be_read_and_says_it_is_not_listed()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var quiz = await CreateQuiz(teacher, Questions(1), Realizam);
            var gameId = await Play(student, quiz, 1);

            await Update(teacher, quiz, isPrivate: true, periodId: Realizam);

            var review = await Review(student, gameId);
            Assert.False(review.GetProperty("quizIsListed").GetBoolean());
            Assert.Equal("Realizam", review.GetProperty("periodName").GetString());
        }

        [Fact]
        public async Task My_games_require_a_session()
        {
            var response = await factory.CreateHttpsClient().GetAsync("/api/quiz/getMyGames");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task My_games_are_the_callers_own_newest_first()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var other = await factory.LoginAsNewStudent();
            var quiz = await CreateQuiz(teacher, Questions(2), Realizam);

            var firstId = await Play(student, quiz, 2, 2);
            var otherId = await Play(other, quiz, 1, 2);
            var secondId = await Play(student, quiz, 1, 1);

            var games = await Games(student);
            Assert.Equal([secondId, firstId], games.Select(g => g.GetProperty("id").GetInt32()));
            Assert.Equal([2, 0], games.Select(g => g.GetProperty("score").GetInt32()));
            Assert.All(games, game =>
            {
                Assert.Equal(quiz.Id, game.GetProperty("quizId").GetInt32());
                Assert.StartsWith("Kviz ", game.GetProperty("quizTitle").GetString());
                Assert.Equal("Realizam", game.GetProperty("periodName").GetString());
                Assert.Equal(2, game.GetProperty("questionCount").GetInt32());
            });
            Assert.Equal(otherId, Assert.Single(await Games(other)).GetProperty("id").GetInt32());
        }

        [Fact]
        public async Task A_teacher_who_played_another_creators_quiz_has_games_too()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var otherTeacher = await factory.LoginAs(ApiFactory.OtherTeacherEmail);
            var quiz = await CreateQuiz(teacher, Questions(1));

            var gameId = await Play(otherTeacher, quiz, 1);

            Assert.Equal(gameId, Assert.Single(await Games(otherTeacher)).GetProperty("id").GetInt32());
        }

        // No other test of the class makes a Quiz in Period 9.
        [Fact]
        public async Task Progress_counts_a_periods_listed_quizzes_and_averages_the_best_play_of_each()
        {
            const int klasicizam = 9;
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var two = await CreateQuiz(teacher, Questions(2), klasicizam);
            var one = await CreateQuiz(teacher, Questions(1), klasicizam);
            var privateQuiz = await CreateQuiz(teacher, Questions(1), klasicizam, isPrivate: true);

            await Play(student, two, 1, 2);
            await Play(student, two, 1, 1);
            var row = Assert.Single(await Progress(student));
            Assert.Equal(klasicizam, row.GetProperty("periodId").GetInt32());
            Assert.Equal("Klasicizam", row.GetProperty("periodName").GetString());
            AssertProgress(row, quizCount: 2, playedCount: 1, averageBestPercent: 100);

            // A worse play afterwards changes nothing: the best one counts.
            await Play(student, two, 2, 2);
            AssertProgress(Assert.Single(await Progress(student)), quizCount: 2, playedCount: 1, averageBestPercent: 100);

            await Play(student, one, 2);
            AssertProgress(Assert.Single(await Progress(student)), quizCount: 2, playedCount: 2, averageBestPercent: 50);

            // A Private Quiz is in the games and in no progress row.
            var privateGameId = await Play(student, privateQuiz, 1);
            AssertProgress(Assert.Single(await Progress(student)), quizCount: 2, playedCount: 2, averageBestPercent: 50);
            Assert.Contains(privateGameId, (await Games(student)).Select(g => g.GetProperty("id").GetInt32()));
        }

        // No other test of the class makes a Quiz in Period 10.
        [Fact]
        public async Task A_quiz_without_a_period_or_no_longer_listed_is_in_the_games_but_not_in_progress()
        {
            const int predromantizam = 10;
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var withoutPeriod = await CreateQuiz(teacher, Questions(1));
            var withPeriod = await CreateQuiz(teacher, Questions(1), predromantizam);
            await Play(student, withoutPeriod, 1);
            await Play(student, withPeriod, 1);

            Assert.Equal(predromantizam, Assert.Single(await Progress(student)).GetProperty("periodId").GetInt32());

            await Update(teacher, withPeriod, isPrivate: true, periodId: predromantizam);

            Assert.Empty(await Progress(student));
            Assert.Equal(2, (await Games(student)).Length);

            // Rows come in curriculum order: Romantizam is before Realizam, though its id is the higher one.
            await Play(student, await CreateQuiz(teacher, Questions(1), Realizam), 1);
            await Play(student, await CreateQuiz(teacher, Questions(1), Romantizam), 1);

            Assert.Equal(["Romantizam", "Realizam"],
                (await Progress(student)).Select(row => row.GetProperty("periodName").GetString()));
        }

        [Fact]
        public async Task Deleting_a_quiz_takes_its_games_out_of_the_players_list()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var quiz = await CreateQuiz(teacher, Questions(1));
            var gameId = await Play(student, quiz, 1);

            var delete = await teacher.DeleteAsync($"/api/quiz/delete/{quiz.Id}");

            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
            Assert.Empty(await Games(student));
            Assert.Equal(HttpStatusCode.NotFound, (await student.GetAsync($"/api/quiz/getGame/{gameId}")).StatusCode);
        }
    }
}
