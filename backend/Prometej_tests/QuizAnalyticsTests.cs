using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Prometej_tests
{
    // The tests of a class share one database, so each test makes a Quiz and signs in Students of its own.
    public class QuizAnalyticsTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        private record CreatedQuiz(int Id, int[] QuestionIds);

        // The options are the four letters, in order, and the first one is correct.
        private static object Question(string title, int correctOption = 1, int id = 0, string options = "ABCD") => new
        {
            id,
            questionTitle = title,
            firstAnswer = options[..1],
            secondAnswer = options[1..2],
            thirdAnswer = options[2..3],
            fourthAnswer = options[3..],
            correctOption,
        };

        private static object[] Questions(int count) =>
            Enumerable.Range(1, count).Select(i => Question($"Pitanje {i}")).ToArray();

        private static async Task<CreatedQuiz> CreateQuiz(HttpClient client, object[] questions)
        {
            var response = await client.PostAsJsonAsync("/api/quiz/create",
                new { quiz = new { title = $"Kviz {Guid.NewGuid():N}", isPrivate = false }, questions });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var id = await response.Content.ReadFromJsonAsync<int>();
            var stored = await client.GetFromJsonAsync<JsonElement>($"/api/quiz/get/{id}");
            return new CreatedQuiz(id, stored.GetProperty("questions").EnumerateArray().Select(q => q.GetProperty("id").GetInt32()).ToArray());
        }

        private static async Task Update(HttpClient client, CreatedQuiz quiz, object[] questions)
        {
            var response = await client.PutAsJsonAsync("/api/quiz/update",
                new { quiz = new { id = quiz.Id, title = "Kviz", isPrivate = false }, questions });

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        // Plays the quiz with the given options, in the order of its questions.
        private static async Task Play(HttpClient client, CreatedQuiz quiz, params int[] chosenOptions)
        {
            var response = await client.PostAsJsonAsync("/api/quiz/submit", new
            {
                quizId = quiz.Id,
                answers = quiz.QuestionIds.Select((questionId, i) => new { questionId, chosenOption = chosenOptions[i] }),
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        private static Task<JsonElement> Analytics(HttpClient client, CreatedQuiz quiz) =>
            client.GetFromJsonAsync<JsonElement>($"/api/quiz/getAnalytics/{quiz.Id}");

        private static JsonElement[] Rows(JsonElement analytics, string name) =>
            analytics.GetProperty(name).EnumerateArray().ToArray();

        private static async Task<int> GetOwnId(HttpClient client) =>
            (await client.GetFromJsonAsync<JsonElement>("/api/user/me")).GetProperty("id").GetInt32();

        private static void AssertQuestion(JsonElement row, string title, int answerCount, int correctCount, string? wrongAnswer, int wrongCount, bool isRetired = false)
        {
            Assert.Equal(title, row.GetProperty("questionTitle").GetString());
            Assert.Equal(answerCount, row.GetProperty("answerCount").GetInt32());
            Assert.Equal(correctCount, row.GetProperty("correctCount").GetInt32());
            Assert.Equal(wrongAnswer, row.GetProperty("mostChosenWrongAnswer").GetString());
            Assert.Equal(wrongCount, row.GetProperty("mostChosenWrongCount").GetInt32());
            Assert.Equal(isRetired, row.GetProperty("isRetired").GetBoolean());
        }

        private static void AssertPlayer(JsonElement row, int userId, int gameCount, (int Score, int Of) first, (int Score, int Of) best)
        {
            Assert.Equal(userId, row.GetProperty("userId").GetInt32());
            Assert.Equal("Sara Student", row.GetProperty("userName").GetString());
            Assert.Equal(gameCount, row.GetProperty("gameCount").GetInt32());
            Assert.Equal(first, (row.GetProperty("firstScore").GetInt32(), row.GetProperty("firstMaxScore").GetInt32()));
            Assert.Equal(best, (row.GetProperty("bestScore").GetInt32(), row.GetProperty("bestMaxScore").GetInt32()));
        }

        [Fact]
        public async Task Analytics_name_the_quiz_and_list_each_games_answers_in_question_order()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var quiz = await CreateQuiz(teacher, Questions(3));
            var submit = await student.PostAsJsonAsync("/api/quiz/submit", new
            {
                quizId = quiz.Id,
                answers = quiz.QuestionIds.Reverse().Select(questionId => new { questionId, chosenOption = 1 }),
            });
            Assert.Equal(HttpStatusCode.Created, submit.StatusCode);

            var analytics = await Analytics(teacher, quiz);

            Assert.StartsWith("Kviz ", analytics.GetProperty("quizTitle").GetString());
            var game = Assert.Single(Rows(analytics, "games"));
            Assert.Equal(["Pitanje 1", "Pitanje 2", "Pitanje 3"],
                game.GetProperty("answers").EnumerateArray().Select(a => a.GetProperty("questionTitle").GetString()));
        }

        [Fact]
        public async Task A_quiz_nobody_played_has_its_questions_and_no_games_or_players()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var quiz = await CreateQuiz(teacher, Questions(2));

            var analytics = await Analytics(teacher, quiz);

            Assert.Empty(Rows(analytics, "games"));
            Assert.Empty(Rows(analytics, "players"));
            var questions = Rows(analytics, "questions");
            Assert.Equal(2, questions.Length);
            Assert.Equal(JsonValueKind.Null, questions[0].GetProperty("mostChosenWrongAnswer").ValueKind);
            AssertQuestion(questions[0], "Pitanje 1", 0, 0, null, 0);
            AssertQuestion(questions[1], "Pitanje 2", 0, 0, null, 0);
        }

        [Fact]
        public async Task The_question_report_counts_every_answer_of_every_play()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var studentA = await factory.LoginAsNewStudent();
            var studentB = await factory.LoginAsNewStudent();
            var quiz = await CreateQuiz(teacher, Questions(2));
            await Play(studentA, quiz, 1, 2);
            await Play(studentA, quiz, 1, 1);
            await Play(studentB, quiz, 2, 2);

            var questions = Rows(await Analytics(teacher, quiz), "questions");

            Assert.Equal(quiz.QuestionIds, questions.Select(q => q.GetProperty("questionId").GetInt32()));
            AssertQuestion(questions[0], "Pitanje 1", 3, 2, "B", 1);
            AssertQuestion(questions[1], "Pitanje 2", 3, 1, "B", 2);
        }

        [Fact]
        public async Task The_question_report_names_the_wrong_answer_chosen_most_often()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);

            async Task<JsonElement> ReportAfter(params int[] chosenByEachStudent)
            {
                var quiz = await CreateQuiz(teacher, Questions(1));
                foreach (var option in chosenByEachStudent)
                {
                    await Play(await factory.LoginAsNewStudent(), quiz, option);
                }

                return Assert.Single(Rows(await Analytics(teacher, quiz), "questions"));
            }

            AssertQuestion(await ReportAfter(2, 3, 3, 1), "Pitanje 1", 4, 1, "C", 2);
            // Two wrong answers chosen equally often: the one that sorts first, here the one chosen earlier.
            AssertQuestion(await ReportAfter(2, 3), "Pitanje 1", 2, 0, "B", 1);
            AssertQuestion(await ReportAfter(1), "Pitanje 1", 1, 1, null, 0);
        }

        [Fact]
        public async Task The_question_report_keeps_a_retired_question_and_lists_a_new_one_with_no_answers()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var quiz = await CreateQuiz(teacher, Questions(2));
            await Play(student, quiz, 1, 2);

            // The second Question was played, so leaving it out retires it.
            await Update(teacher, quiz, [Question("Pitanje 1", id: quiz.QuestionIds[0]), Question("Pitanje 3")]);

            var questions = Rows(await Analytics(teacher, quiz), "questions");
            Assert.Equal(3, questions.Length);
            AssertQuestion(questions[0], "Pitanje 1", 1, 1, null, 0);
            AssertQuestion(questions[1], "Pitanje 2", 1, 0, "B", 1, isRetired: true);
            AssertQuestion(questions[2], "Pitanje 3", 0, 0, null, 0);
        }

        [Fact]
        public async Task The_question_report_shows_todays_title_over_answers_counted_as_they_were_played()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var quiz = await CreateQuiz(teacher, [Question("Staro pitanje")]);
            await Play(await factory.LoginAsNewStudent(), quiz, 1);

            // Option 1 was right when the first Student chose it and is wrong for the second.
            await Update(teacher, quiz, [Question("Novo pitanje", correctOption: 3, id: quiz.QuestionIds[0], options: "WXYZ")]);
            await Play(await factory.LoginAsNewStudent(), quiz, 1);

            var question = Assert.Single(Rows(await Analytics(teacher, quiz), "questions"));
            AssertQuestion(question, "Novo pitanje", 2, 1, "W", 1);
        }

        [Fact]
        public async Task A_player_has_one_row_with_their_games_their_first_and_their_best()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var studentA = await factory.LoginAsNewStudent();
            var studentB = await factory.LoginAsNewStudent();
            var quiz = await CreateQuiz(teacher, Questions(2));
            await Play(studentA, quiz, 2, 2);
            await Play(studentA, quiz, 1, 1);
            await Play(studentB, quiz, 1, 2);
            await Play(studentA, quiz, 1, 2);

            var analytics = await Analytics(teacher, quiz);

            // A's last play is the newest, so A comes first.
            var players = Rows(analytics, "players");
            Assert.Equal(2, players.Length);
            AssertPlayer(players[0], await GetOwnId(studentA), gameCount: 3, first: (0, 2), best: (2, 2));
            AssertPlayer(players[1], await GetOwnId(studentB), gameCount: 1, first: (1, 2), best: (1, 2));
            Assert.Equal(Rows(analytics, "games")[0].GetProperty("datePlayed").GetDateTime(),
                players[0].GetProperty("lastPlayed").GetDateTime());
        }

        [Fact]
        public async Task A_players_best_is_the_highest_share_when_the_quiz_changed_between_plays()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var quiz = await CreateQuiz(teacher, Questions(2));
            await Play(student, quiz, 1, 2);

            // The same score of 1 is half of the first play and all of the second.
            await Update(teacher, quiz, [Question("Pitanje 1", id: quiz.QuestionIds[0])]);
            await Play(student, quiz with { QuestionIds = [quiz.QuestionIds[0]] }, 1);

            var player = Assert.Single(Rows(await Analytics(teacher, quiz), "players"));
            AssertPlayer(player, await GetOwnId(student), gameCount: 2, first: (1, 2), best: (1, 1));
        }
    }
}
