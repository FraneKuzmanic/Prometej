using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Prometej_tests
{
    public class OrderingQuestionTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        // Five works in the order they were written; a play names them by these numbers, 1 to 5.
        private static readonly string[] Works = ["Judita", "Planine", "Dundo Maroje", "Osman", "Smrt Smail-age Čengića"];

        private static object Ordering(string[]? items = null, int id = 0) => new
        {
            id,
            type = "ordering",
            questionTitle = "Poredaj djela po vremenu nastanka.",
            content = new { items = items ?? Works },
        };

        private static string[] Items(int count) => Enumerable.Range(1, count).Select(i => $"Pojam {i}").ToArray();

        private static Task<HttpResponseMessage> Create(HttpClient client, params object?[] questions) =>
            client.PostAsJsonAsync("/api/quiz/create", new { quiz = new { title = "Kviz", isPrivate = false }, questions });

        private static async Task<JsonElement> Created(HttpClient client, params object[] questions)
        {
            var response = await Create(client, questions);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return await Get(client, await response.Content.ReadFromJsonAsync<int>());
        }

        private static Task<HttpResponseMessage> Update(HttpClient client, int quizId, params object[] questions) =>
            client.PutAsJsonAsync("/api/quiz/update", new { quiz = new { id = quizId, title = "Kviz", isPrivate = false }, questions });

        private static Task<JsonElement> Get(HttpClient client, int quizId) =>
            client.GetFromJsonAsync<JsonElement>($"/api/quiz/get/{quizId}");

        private static int Id(JsonElement element) => element.GetProperty("id").GetInt32();

        private static int QuestionId(JsonElement quiz) => Id(quiz.GetProperty("questions")[0]);

        private static Task<HttpResponseMessage> Submit(HttpClient client, JsonElement quiz, params int?[] order) =>
            client.PostAsJsonAsync("/api/quiz/submit", new { quizId = Id(quiz), answers = new[] { new { questionId = QuestionId(quiz), order } } });

        private static async Task<JsonElement> Played(HttpClient client, JsonElement quiz, params int?[] order)
        {
            var response = await Submit(client, quiz, order);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }

        private static Task<JsonElement> Analytics(HttpClient client, JsonElement quiz) =>
            client.GetFromJsonAsync<JsonElement>($"/api/quiz/getAnalytics/{Id(quiz)}");

        private static string?[] Texts(JsonElement game, string property) =>
            game.GetProperty("answers").EnumerateArray().Select(answer => answer.GetProperty(property).GetString()).ToArray();

        [Fact]
        public async Task An_ordering_question_is_saved_and_read_back_with_its_items_in_order()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);

            var quiz = await Created(teacher, Ordering([" Judita ", "Planine", "Osman"]));

            var question = quiz.GetProperty("questions")[0];
            Assert.Equal("ordering", question.GetProperty("type").GetString());
            Assert.Equal(["Judita", "Planine", "Osman"],
                question.GetProperty("content").GetProperty("items").EnumerateArray().Select(item => item.GetString()));
            Assert.Equal(JsonValueKind.Null, question.GetProperty("firstAnswer").ValueKind);
        }

        [Theory]
        [InlineData(2, HttpStatusCode.BadRequest)]
        [InlineData(3, HttpStatusCode.Created)]
        [InlineData(6, HttpStatusCode.Created)]
        [InlineData(7, HttpStatusCode.BadRequest)]
        public async Task An_ordering_question_needs_three_to_six_items(int itemCount, HttpStatusCode expected)
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);

            var response = await Create(teacher, Ordering(Items(itemCount)));

            Assert.Equal(expected, response.StatusCode);
        }

        [Fact]
        public async Task An_ordering_question_with_a_wrong_shape_is_refused()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var items = Items(3);

            var cases = new (string Name, object Question)[]
            {
                ("two equal items", Ordering(["Isto", "Isto", "Drugo"])),
                ("items that differ only by spaces", Ordering(["Isto", " Isto ", "Drugo"])),
                ("a blank item", Ordering(["Prvo", "  ", "Treće"])),
                ("an item of 201 characters", Ordering(["Prvo", "Drugo", new string('a', 201)])),
                ("a null item", new { type = "ordering", questionTitle = "Pitanje", content = new { items = new string?[] { "Prvo", null, "Treće" } } }),
                ("no content", new { type = "ordering", questionTitle = "Pitanje" }),
                ("pairs next to the items", new { type = "ordering", questionTitle = "Pitanje", content = new { items, pairs = new[] { new { left = "A", right = "B" } } } }),
                ("extras next to the items", new { type = "ordering", questionTitle = "Pitanje", content = new { items, extras = new[] { "X" } } }),
                ("answer options", new { type = "ordering", questionTitle = "Pitanje", firstAnswer = "A", content = new { items } }),
            };
            foreach (var (name, question) in cases)
            {
                var response = await Create(teacher, question);

                Assert.True(response.StatusCode == HttpStatusCode.BadRequest, $"{name}: {response.StatusCode}");
            }
        }

        [Fact]
        public async Task An_unplayed_ordering_question_that_is_edited_is_read_back_changed()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var quiz = await Created(teacher, Ordering());

            var response = await Update(teacher, Id(quiz), Ordering(["Osman", "Judita", "Planine", "Robinja"], QuestionId(quiz)));

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            var stored = await Get(teacher, Id(quiz));
            Assert.Equal(QuestionId(quiz), QuestionId(stored));
            Assert.Equal(["Osman", "Judita", "Planine", "Robinja"],
                stored.GetProperty("questions")[0].GetProperty("content").GetProperty("items").EnumerateArray().Select(item => item.GetString()));
        }

        [Fact]
        public async Task Each_item_at_its_place_is_a_point()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var quiz = await Created(teacher, Ordering());

            // The second and the fourth work are swapped.
            var game = await Played(student, quiz, 1, 4, 3, 2, 5);

            Assert.Equal(3, game.GetProperty("score").GetInt32());
            Assert.Equal([1, 2, 3, 4, 5], game.GetProperty("answers").EnumerateArray().Select(answer => answer.GetProperty("place").GetInt32()));
            Assert.Equal(["Judita", "Osman", "Dundo Maroje", "Planine", "Smrt Smail-age Čengića"], Texts(game, "answerText"));
            Assert.Equal(Works, Texts(game, "correctAnswer"));
            Assert.All(game.GetProperty("answers").EnumerateArray(), answer => Assert.Equal(JsonValueKind.Null, answer.GetProperty("item").ValueKind));
        }

        [Fact]
        public async Task The_right_order_scores_every_point_and_a_rotation_scores_none()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var quiz = await Created(teacher, Ordering());

            var right = await Played(student, quiz, 1, 2, 3, 4, 5);
            // One item put first by mistake moves every other one off its place.
            var shifted = await Played(student, quiz, 5, 1, 2, 3, 4);

            Assert.Equal(5, right.GetProperty("score").GetInt32());
            Assert.Equal(0, shifted.GetProperty("score").GetInt32());
        }

        public static TheoryData<string, int?[]> BadOrders => new()
        {
            { "too short", [1, 2, 3, 4] },
            { "too long", [1, 2, 3, 4, 5, 5] },
            { "a number twice", [1, 1, 3, 4, 5] },
            { "zero", [0, 2, 3, 4, 5] },
            { "a number past the items", [1, 2, 3, 4, 6] },
            { "a null", [1, 2, null, 4, 5] },
            { "more than a question can have", [1, 2, 3, 4, 5, 6, 7] },
        };

        [Theory]
        [MemberData(nameof(BadOrders))]
        public async Task An_order_that_does_not_name_every_item_once_is_refused(string name, int?[] order)
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var quiz = await Created(teacher, Ordering());

            var response = await Submit(student, quiz, order);

            Assert.True(response.StatusCode == HttpStatusCode.BadRequest, $"{name}: {response.StatusCode}");
            Assert.Equal(0, (await Analytics(teacher, quiz)).GetProperty("games").GetArrayLength());
        }

        [Fact]
        public async Task An_option_or_matches_for_an_ordering_question_are_refused()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var quiz = await Created(teacher, Ordering());
            var questionId = QuestionId(quiz);

            var option = await student.PostAsJsonAsync("/api/quiz/submit", new { quizId = Id(quiz), answers = new[] { new { questionId, chosenOption = 1 } } });
            var matches = await student.PostAsJsonAsync("/api/quiz/submit", new { quizId = Id(quiz), answers = new[] { new { questionId, matches = new[] { 1, 2, 3, 4, 5 } } } });

            Assert.Equal(HttpStatusCode.BadRequest, option.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, matches.StatusCode);
        }

        [Fact]
        public async Task The_question_report_has_a_line_for_each_place()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var quiz = await Created(teacher, Ordering(["Judita", "Planine", "Osman"]));
            await Played(await factory.LoginAsNewStudent(), quiz, 1, 2, 3);
            await Played(await factory.LoginAsNewStudent(), quiz, 1, 3, 2);
            await Played(await factory.LoginAsNewStudent(), quiz, 1, 3, 2);

            var row = Assert.Single((await Analytics(teacher, quiz)).GetProperty("questions").EnumerateArray());

            Assert.Equal("ordering", row.GetProperty("type").GetString());
            Assert.Equal(9, row.GetProperty("answerCount").GetInt32());
            Assert.Equal(5, row.GetProperty("correctCount").GetInt32());
            Assert.Equal(JsonValueKind.Null, row.GetProperty("mostChosenWrongAnswer").ValueKind);
            var lines = row.GetProperty("lines").EnumerateArray().ToArray();
            Assert.Equal(["1", "2", "3"], lines.Select(line => line.GetProperty("label").GetString()));
            Assert.Equal([3, 1, 1], lines.Select(line => line.GetProperty("correctCount").GetInt32()));
            Assert.Equal([null, "Osman", "Planine"], lines.Select(line => line.GetProperty("mostChosenWrongAnswer").GetString()));
            Assert.Equal([0, 2, 2], lines.Select(line => line.GetProperty("mostChosenWrongCount").GetInt32()));
        }

        [Fact]
        public async Task Editing_a_played_ordering_question_does_not_change_its_stored_answers()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var quiz = await Created(teacher, Ordering(["Judita", "Planine", "Osman"]));
            var game = await Played(student, quiz, 1, 3, 2);

            var response = await Update(teacher, Id(quiz), Ordering(["Osman", "Planine", "Judita", "Robinja"], QuestionId(quiz)));
            var newGame = await Played(student, quiz, 1, 2, 3, 4);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            var review = await student.GetFromJsonAsync<JsonElement>($"/api/quiz/getGame/{Id(game)}");
            Assert.Equal(1, review.GetProperty("score").GetInt32());
            Assert.Equal(["Judita", "Osman", "Planine"], Texts(review, "answerText"));
            Assert.Equal(["Judita", "Planine", "Osman"], Texts(review, "correctAnswer"));
            Assert.Equal(4, newGame.GetProperty("score").GetInt32());
            Assert.Equal(["Osman", "Planine", "Judita", "Robinja"], Texts(newGame, "correctAnswer"));
        }

        [Fact]
        public async Task A_play_of_all_three_kinds_is_scored_in_points()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var quiz = await Created(teacher,
                new { questionTitle = "Izbor", firstAnswer = "A", secondAnswer = "B", thirdAnswer = "C", fourthAnswer = "D", correctOption = 1 },
                new
                {
                    type = "matching",
                    questionTitle = "Povezivanje",
                    content = new { pairs = new[] { new { left = "1", right = "A" }, new { left = "2", right = "B" }, new { left = "3", right = "C" } } },
                },
                Ordering(["Judita", "Planine", "Osman"]));
            var ids = quiz.GetProperty("questions").EnumerateArray().Select(Id).ToArray();
            var key = Guid.NewGuid();
            var body = new
            {
                quizId = Id(quiz),
                submissionKey = key,
                answers = new object[]
                {
                    new { questionId = ids[0], chosenOption = 2 },
                    new { questionId = ids[1], matches = new[] { 1, 3, 2 } },
                    new { questionId = ids[2], order = new[] { 1, 2, 3 } },
                },
            };

            var response = await student.PostAsJsonAsync("/api/quiz/submit", body);
            var again = await student.PostAsJsonAsync("/api/quiz/submit", body);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var game = await response.Content.ReadFromJsonAsync<JsonElement>();
            // 0 of 1, 1 of 3 and 3 of 3.
            Assert.Equal(4, game.GetProperty("score").GetInt32());
            Assert.Equal(7, game.GetProperty("answers").GetArrayLength());
            Assert.Equal([0, 1, 2, 3, 4, 5, 6], game.GetProperty("answers").EnumerateArray().Select(answer => answer.GetProperty("position").GetInt32()));
            var repeated = await again.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(Id(game), Id(repeated));
            Assert.Equal([0, 1, 2, 3, 4, 5, 6], repeated.GetProperty("answers").EnumerateArray().Select(answer => answer.GetProperty("position").GetInt32()));
            Assert.Equal(1, (await Analytics(teacher, quiz)).GetProperty("games").GetArrayLength());
        }
    }
}
