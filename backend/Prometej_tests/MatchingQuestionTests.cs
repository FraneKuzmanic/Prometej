using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Prometej_tests
{
    public class MatchingQuestionTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        private const int Realizam = 4;

        // Four works and their authors; the right-hand options are numbered 1 to 4 in this
        // order, and the one extra is number 5.
        private static readonly (string Left, string Right)[] Works =
        [
            ("Judita", "Marko Marulić"),
            ("Planine", "Petar Zoranić"),
            ("Dundo Maroje", "Marin Držić"),
            ("Osman", "Ivan Gundulić"),
        ];

        private static object Choice(string title = "Izbor", int id = 0) => new
        {
            id,
            questionTitle = title,
            firstAnswer = "A",
            secondAnswer = "B",
            thirdAnswer = "C",
            fourthAnswer = "D",
            correctOption = 1,
        };

        private static object Matching((string Left, string Right)[]? pairs = null, string[]? extras = null, int id = 0) => new
        {
            id,
            type = "matching",
            questionTitle = "Poveži djelo s autorom.",
            content = new
            {
                pairs = (pairs ?? Works).Select(pair => new { left = pair.Left, right = pair.Right }),
                extras = extras ?? ["Hanibal Lucić"],
            },
        };

        private static (string, string)[] Pairs(int count) =>
            Enumerable.Range(1, count).Select(i => ($"Lijevo {i}", $"Desno {i}")).ToArray();

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

        private static int[] QuestionIds(JsonElement quiz) =>
            quiz.GetProperty("questions").EnumerateArray().Select(Id).ToArray();

        private static object Matches(int questionId, params int[] matches) => new { questionId, matches };

        private static Task<HttpResponseMessage> Submit(HttpClient client, int quizId, params object[] answers) =>
            client.PostAsJsonAsync("/api/quiz/submit", new { quizId, answers });

        private static async Task<JsonElement> Played(HttpClient client, int quizId, params object[] answers)
        {
            var response = await Submit(client, quizId, answers);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }

        private static Task<JsonElement> Analytics(HttpClient client, int quizId) =>
            client.GetFromJsonAsync<JsonElement>($"/api/quiz/getAnalytics/{quizId}");

        private static string?[] Texts(JsonElement game, string property) =>
            game.GetProperty("answers").EnumerateArray().Select(answer => answer.GetProperty(property).GetString()).ToArray();

        [Fact]
        public async Task A_matching_question_is_saved_and_read_back_with_its_pairs_and_extras()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);

            var quiz = await Created(teacher, Matching());

            var question = quiz.GetProperty("questions")[0];
            Assert.Equal("matching", question.GetProperty("type").GetString());
            var content = question.GetProperty("content");
            Assert.Equal(Works.Select(pair => pair.Left), content.GetProperty("pairs").EnumerateArray().Select(pair => pair.GetProperty("left").GetString()));
            Assert.Equal(Works.Select(pair => pair.Right), content.GetProperty("pairs").EnumerateArray().Select(pair => pair.GetProperty("right").GetString()));
            Assert.Equal(["Hanibal Lucić"], content.GetProperty("extras").EnumerateArray().Select(extra => extra.GetString()));
            Assert.Equal(JsonValueKind.Null, question.GetProperty("firstAnswer").ValueKind);
            Assert.Equal(JsonValueKind.Null, question.GetProperty("correctOption").ValueKind);
        }

        [Fact]
        public async Task Its_texts_are_stored_trimmed_and_it_needs_no_extras()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);

            var quiz = await Created(teacher, Matching([(" Judita ", " Marko Marulić "), ("Planine", "Petar Zoranić"), ("Osman", "Ivan Gundulić")], []));

            var content = quiz.GetProperty("questions")[0].GetProperty("content");
            Assert.Equal("Judita", content.GetProperty("pairs")[0].GetProperty("left").GetString());
            Assert.Equal("Marko Marulić", content.GetProperty("pairs")[0].GetProperty("right").GetString());
            Assert.False(content.TryGetProperty("extras", out var extras) && extras.ValueKind != JsonValueKind.Null);
        }

        [Fact]
        public async Task A_request_without_a_type_is_a_choice_question()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);

            var quiz = await Created(teacher, Choice());

            var question = quiz.GetProperty("questions")[0];
            Assert.Equal("choice", question.GetProperty("type").GetString());
            Assert.Equal(JsonValueKind.Null, question.GetProperty("content").ValueKind);
        }

        [Theory]
        [InlineData(2)]
        [InlineData(6)]
        public async Task A_matching_question_needs_three_to_five_pairs(int pairCount)
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);

            var response = await Create(teacher, Matching(Pairs(pairCount), []));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Three_and_five_pairs_with_two_extras_are_accepted_and_three_extras_are_not()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);

            Assert.Equal(HttpStatusCode.Created, (await Create(teacher, Matching(Pairs(3), ["X", "Y"]))).StatusCode);
            Assert.Equal(HttpStatusCode.Created, (await Create(teacher, Matching(Pairs(5), ["X", "Y"]))).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await Create(teacher, Matching(Pairs(3), ["X", "Y", "Z"]))).StatusCode);
        }

        public static TheoryData<string, string[][], string[]> SameTexts => new()
        {
            { "two equal left items", [["Isto", "A"], ["Isto", "B"], ["Treće", "C"]], [] },
            { "left items that differ only by spaces", [["Isto", "A"], [" Isto ", "B"], ["Treće", "C"]], [] },
            { "two equal right options", [["Prvo", "Isto"], ["Drugo", "Isto"], ["Treće", "C"]], [] },
            { "an extra equal to a right option", [["Prvo", "A"], ["Drugo", "B"], ["Treće", "C"]], [" A "] },
            { "two equal extras", [["Prvo", "A"], ["Drugo", "B"], ["Treće", "C"]], ["X", "X"] },
        };

        [Theory]
        [MemberData(nameof(SameTexts))]
        public async Task Its_left_items_must_differ_and_so_must_its_right_options_and_extras(string name, string[][] pairs, string[] extras)
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);

            var response = await Create(teacher, Matching(pairs.Select(pair => (pair[0], pair[1])).ToArray(), extras));

            Assert.True(response.StatusCode == HttpStatusCode.BadRequest, $"{name}: {response.StatusCode}");
            Assert.Contains("must all differ", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Right_options_that_differ_in_case_are_different_options()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);

            var response = await Create(teacher, Matching([("Prvo", "roman"), ("Drugo", "Roman"), ("Treće", "ROMAN")], []));

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task A_text_that_is_blank_or_longer_than_200_characters_is_refused()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var longest = new string('a', 200);
            var tooLong = new string('a', 201);
            (string, string)[] With(string left, string right) => [(left, right), ("Drugo", "B"), ("Treće", "C")];

            var cases = new (string Name, object Question)[]
            {
                ("a long left item", Matching(With(tooLong, "A"), [])),
                ("a long right option", Matching(With("Prvo", tooLong), [])),
                ("a long extra", Matching(With("Prvo", "A"), [tooLong])),
                ("a blank left item", Matching(With("  ", "A"), [])),
                ("an empty right option", Matching(With("Prvo", ""), [])),
                ("a blank extra", Matching(With("Prvo", "A"), [" "])),
            };
            foreach (var (name, question) in cases)
            {
                var response = await Create(teacher, question);

                Assert.True(response.StatusCode == HttpStatusCode.BadRequest, $"{name}: {response.StatusCode}");
            }

            Assert.Equal(HttpStatusCode.Created, (await Create(teacher, Matching(With(longest, longest), [longest[..199]]))).StatusCode);
        }

        [Fact]
        public async Task A_question_that_carries_what_its_type_does_not_have_is_refused()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var pairs = Works.Select(pair => new { left = pair.Left, right = pair.Right });

            var cases = new (string Name, object? Question)[]
            {
                ("a matching question with answer options", new
                {
                    type = "matching", questionTitle = "Pitanje", firstAnswer = "A", secondAnswer = "B", thirdAnswer = "C",
                    fourthAnswer = "D", correctOption = 1, content = new { pairs },
                }),
                ("a matching question with a correct option", new { type = "matching", questionTitle = "Pitanje", correctOption = 1, content = new { pairs } }),
                ("a matching question with items", new { type = "matching", questionTitle = "Pitanje", content = new { pairs, items = new[] { "A", "B", "C" } } }),
                ("a matching question without content", new { type = "matching", questionTitle = "Pitanje" }),
                ("a matching question with null content", new { type = "matching", questionTitle = "Pitanje", content = (object?)null }),
                ("a matching question with a null pair", new { type = "matching", questionTitle = "Pitanje", content = new { pairs = pairs.Cast<object?>().Append(null) } }),
                ("a matching question with a null extra", new { type = "matching", questionTitle = "Pitanje", content = new { pairs, extras = new string?[] { null } } }),
                ("a matching question with a pair without a right side", new { type = "matching", questionTitle = "Pitanje", content = new { pairs = new[] { new { left = "A" }, new { left = "B" }, new { left = "C" } } } }),
                ("a choice question with content", new
                {
                    questionTitle = "Pitanje", firstAnswer = "A", secondAnswer = "B", thirdAnswer = "C", fourthAnswer = "D",
                    correctOption = 1, content = new { pairs },
                }),
                ("a choice question without its answers", new { type = "choice", questionTitle = "Pitanje" }),
                ("an unknown type", new { type = "cloze", questionTitle = "Pitanje", content = new { pairs } }),
                ("a null type", new
                {
                    type = (string?)null, questionTitle = "Pitanje", firstAnswer = "A", secondAnswer = "B", thirdAnswer = "C",
                    fourthAnswer = "D", correctOption = 1,
                }),
            };
            foreach (var (name, question) in cases)
            {
                var response = await Create(teacher, question);

                Assert.True(response.StatusCode == HttpStatusCode.BadRequest, $"{name}: {response.StatusCode}");
            }
        }

        [Fact]
        public async Task A_matching_question_cannot_be_asked_about_a_source_text()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);

            var response = await teacher.PostAsJsonAsync("/api/quiz/create", new
            {
                quiz = new { title = "Kviz", isPrivate = false },
                sourceTexts = new[] { new { caption = "Naslov", body = "Tekst" } },
                questions = new object[]
                {
                    Choice(),
                    new
                    {
                        type = "matching", questionTitle = "Pitanje", sourceTextNo = 1,
                        content = new { pairs = Works.Select(pair => new { left = pair.Left, right = pair.Right }) },
                    },
                },
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task A_questions_type_cannot_be_changed()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var quiz = await Created(teacher, Choice(), Matching());
            var ids = QuestionIds(quiz);

            var toMatching = await Update(teacher, Id(quiz), Matching(id: ids[0]), Matching(id: ids[1]));
            var toChoice = await Update(teacher, Id(quiz), Choice(id: ids[0]), Choice(id: ids[1]));

            Assert.Equal(HttpStatusCode.BadRequest, toMatching.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, toChoice.StatusCode);
            Assert.Contains("type cannot be changed", await toChoice.Content.ReadAsStringAsync());
            Assert.Equal(["choice", "matching"],
                (await Get(teacher, Id(quiz))).GetProperty("questions").EnumerateArray().Select(q => q.GetProperty("type").GetString()));
        }

        [Fact]
        public async Task An_unplayed_matching_question_that_is_edited_is_read_back_changed()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var quiz = await Created(teacher, Matching());
            (string, string)[] edited = [("Judita", "Marko Marulić"), ("Robinja", "Hanibal Lucić"), ("Ribanje i ribarsko prigovaranje", "Petar Hektorović")];

            var response = await Update(teacher, Id(quiz), Matching(edited, ["Marin Držić", "Ivan Gundulić"], QuestionIds(quiz)[0]));

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            var stored = await Get(teacher, Id(quiz));
            Assert.Equal(QuestionIds(quiz), QuestionIds(stored));
            var content = stored.GetProperty("questions")[0].GetProperty("content");
            Assert.Equal(["Judita", "Robinja", "Ribanje i ribarsko prigovaranje"],
                content.GetProperty("pairs").EnumerateArray().Select(pair => pair.GetProperty("left").GetString()));
            Assert.Equal(["Marko Marulić", "Hanibal Lucić", "Petar Hektorović"],
                content.GetProperty("pairs").EnumerateArray().Select(pair => pair.GetProperty("right").GetString()));
            Assert.Equal(["Marin Držić", "Ivan Gundulić"], content.GetProperty("extras").EnumerateArray().Select(extra => extra.GetString()));
        }

        [Fact]
        public async Task A_quiz_stored_without_questions_can_gain_a_matching_question()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var ownId = (await teacher.GetFromJsonAsync<JsonElement>("/api/user/me")).GetProperty("id").GetInt32();
            var quizId = factory.AddLegacyQuiz(ownId);

            var response = await Update(teacher, quizId, Matching());

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal("matching", (await Get(teacher, quizId)).GetProperty("questions")[0].GetProperty("type").GetString());
        }

        [Fact]
        public async Task Each_right_pair_is_a_point()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var quiz = await Created(teacher, Matching());

            // The last two authors are swapped.
            var game = await Played(student, Id(quiz), Matches(QuestionIds(quiz)[0], 1, 2, 4, 3));

            Assert.Equal(2, game.GetProperty("score").GetInt32());
            Assert.Equal(["Judita", "Planine", "Dundo Maroje", "Osman"], Texts(game, "item"));
            Assert.Equal(["Marko Marulić", "Petar Zoranić", "Ivan Gundulić", "Marin Držić"], Texts(game, "answerText"));
            Assert.Equal(["Marko Marulić", "Petar Zoranić", "Marin Držić", "Ivan Gundulić"], Texts(game, "correctAnswer"));
            Assert.Equal([0, 1, 2, 3], game.GetProperty("answers").EnumerateArray().Select(answer => answer.GetProperty("position").GetInt32()));
            Assert.All(game.GetProperty("answers").EnumerateArray(), answer =>
            {
                Assert.Equal("Poveži djelo s autorom.", answer.GetProperty("questionTitle").GetString());
                Assert.Equal(JsonValueKind.Null, answer.GetProperty("place").ValueKind);
            });
        }

        [Fact]
        public async Task An_extra_option_can_be_chosen_and_is_wrong()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var quiz = await Created(teacher, Matching());

            var game = await Played(student, Id(quiz), Matches(QuestionIds(quiz)[0], 5, 2, 3, 4));

            Assert.Equal(3, game.GetProperty("score").GetInt32());
            Assert.Equal("Hanibal Lucić", game.GetProperty("answers")[0].GetProperty("answerText").GetString());
            Assert.Equal("Marko Marulić", game.GetProperty("answers")[0].GetProperty("correctAnswer").GetString());
        }

        public static TheoryData<string, int?[]> BadMatches => new()
        {
            { "too few", [1, 2, 3] },
            { "too many", [1, 2, 3, 4, 5] },
            { "a number twice", [1, 1, 3, 4] },
            { "zero", [0, 2, 3, 4] },
            { "a number past the extras", [6, 2, 3, 4] },
            { "a null", [1, null, 3, 4] },
            { "more than a question can have", [1, 2, 3, 4, 5, 6] },
        };

        [Theory]
        [MemberData(nameof(BadMatches))]
        public async Task A_partial_or_repeated_match_is_refused_and_stores_nothing(string name, int?[] matches)
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var quiz = await Created(teacher, Matching());

            var response = await Submit(student, Id(quiz), new { questionId = QuestionIds(quiz)[0], matches });

            Assert.True(response.StatusCode == HttpStatusCode.BadRequest, $"{name}: {response.StatusCode}");
            Assert.Equal(0, (await Analytics(teacher, Id(quiz))).GetProperty("games").GetArrayLength());
        }

        [Fact]
        public async Task An_answer_of_another_types_kind_is_refused()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var quiz = await Created(teacher, Choice(), Matching());
            var ids = QuestionIds(quiz);
            object option = new { questionId = ids[0], chosenOption = 1 };
            object matches = Matches(ids[1], 1, 2, 3, 4);

            var cases = new (string Name, object[] Answers)[]
            {
                ("matches for a choice question", [Matches(ids[0], 1, 2, 3, 4), matches]),
                ("an option for a matching question", [option, new { questionId = ids[1], chosenOption = 1 }]),
                ("an order for a matching question", [option, new { questionId = ids[1], order = new[] { 1, 2, 3, 4 } }]),
                ("an option and matches together", [option, new { questionId = ids[1], chosenOption = 1, matches = new[] { 1, 2, 3, 4 } }]),
                ("nothing", [option, new { questionId = ids[1] }]),
            };
            foreach (var (name, answers) in cases)
            {
                var response = await Submit(student, Id(quiz), answers);

                Assert.True(response.StatusCode == HttpStatusCode.BadRequest, $"{name}: {response.StatusCode}");
            }

            Assert.Equal(0, (await Analytics(teacher, Id(quiz))).GetProperty("games").GetArrayLength());
            Assert.Equal(HttpStatusCode.Created, (await Submit(student, Id(quiz), option, matches)).StatusCode);
        }

        [Fact]
        public async Task A_mixed_quiz_scores_points_over_all_its_rows()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var response = await teacher.PostAsJsonAsync("/api/quiz/create", new
            {
                quiz = new { title = "Kviz", isPrivate = false, periodId = Realizam },
                questions = new[] { Choice(), Matching() },
            });
            var quiz = await Get(teacher, await response.Content.ReadFromJsonAsync<int>());
            var ids = QuestionIds(quiz);

            // The request names the matching question first; the rows follow the quiz.
            var game = await Played(student, Id(quiz), Matches(ids[1], 1, 2, 4, 3), new { questionId = ids[0], chosenOption = 1 });

            Assert.Equal(3, game.GetProperty("score").GetInt32());
            Assert.Equal(5, game.GetProperty("answers").GetArrayLength());
            Assert.Equal([null, "Judita", "Planine", "Dundo Maroje", "Osman"], Texts(game, "item"));
            var mine = await student.GetFromJsonAsync<JsonElement>("/api/quiz/getMyGames");
            var played = Assert.Single(mine.GetProperty("games").EnumerateArray());
            Assert.Equal(3, played.GetProperty("score").GetInt32());
            Assert.Equal(5, played.GetProperty("maxScore").GetInt32());
            Assert.Equal(60, Assert.Single(mine.GetProperty("progress").EnumerateArray()).GetProperty("averageBestPercent").GetInt32());
            var player = Assert.Single((await Analytics(teacher, Id(quiz))).GetProperty("players").EnumerateArray());
            Assert.Equal(5, player.GetProperty("firstMaxScore").GetInt32());
            Assert.Equal(5, player.GetProperty("bestMaxScore").GetInt32());
            // A card still counts questions, not points.
            var card = (await teacher.GetFromJsonAsync<JsonElement>("/api/quiz/getMyQuizzes")).EnumerateArray().Single(q => Id(q) == Id(quiz));
            Assert.Equal(2, card.GetProperty("questionCount").GetInt32());
        }

        [Fact]
        public async Task Editing_a_played_matching_question_does_not_change_its_stored_answers()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var quiz = await Created(teacher, Matching());
            var questionId = QuestionIds(quiz)[0];
            var game = await Played(student, Id(quiz), Matches(questionId, 1, 2, 4, 3));
            (string, string)[] edited = [("Judita (1501.)", "Marulić"), ("Planine", "Zoranić"), ("Dundo Maroje", "Držić")];

            var response = await Update(teacher, Id(quiz), Matching(edited, [], questionId));
            var newGame = await Played(student, Id(quiz), Matches(questionId, 1, 2, 3));

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            var review = await student.GetFromJsonAsync<JsonElement>($"/api/quiz/getGame/{Id(game)}");
            Assert.Equal(2, review.GetProperty("score").GetInt32());
            Assert.Equal(["Judita", "Planine", "Dundo Maroje", "Osman"], Texts(review, "item"));
            Assert.Equal(["Marko Marulić", "Petar Zoranić", "Ivan Gundulić", "Marin Držić"], Texts(review, "answerText"));
            Assert.Equal(3, newGame.GetProperty("score").GetInt32());
            Assert.Equal(["Judita (1501.)", "Planine", "Dundo Maroje"], Texts(newGame, "item"));
        }

        [Fact]
        public async Task A_removed_played_matching_question_is_retired()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var quiz = await Created(teacher, Choice(), Matching());
            var ids = QuestionIds(quiz);
            var game = await Played(student, Id(quiz), new { questionId = ids[0], chosenOption = 1 }, Matches(ids[1], 1, 2, 3, 4));

            var response = await Update(teacher, Id(quiz), Choice(id: ids[0]));

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal([ids[0]], QuestionIds(await Get(teacher, Id(quiz))));
            var analytics = await Analytics(teacher, Id(quiz));
            var retired = analytics.GetProperty("questions").EnumerateArray().Single(q => q.GetProperty("questionId").GetInt32() == ids[1]);
            Assert.True(retired.GetProperty("isRetired").GetBoolean());
            Assert.Equal(4, retired.GetProperty("lines").GetArrayLength());
            var review = await student.GetFromJsonAsync<JsonElement>($"/api/quiz/getGame/{Id(game)}");
            Assert.Equal(5, review.GetProperty("score").GetInt32());
            Assert.Equal(5, review.GetProperty("answers").GetArrayLength());
        }

        [Fact]
        public async Task The_question_report_has_a_line_for_each_left_item_with_its_most_chosen_wrong_match()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var quiz = await Created(teacher, Choice(), Matching());
            var ids = QuestionIds(quiz);
            object option = new { questionId = ids[0], chosenOption = 2 };
            await Played(await factory.LoginAsNewStudent(), Id(quiz), option, Matches(ids[1], 1, 2, 3, 4));
            await Played(await factory.LoginAsNewStudent(), Id(quiz), option, Matches(ids[1], 5, 2, 4, 3));
            await Played(await factory.LoginAsNewStudent(), Id(quiz), option, Matches(ids[1], 5, 2, 4, 3));

            var rows = (await Analytics(teacher, Id(quiz))).GetProperty("questions").EnumerateArray().ToArray();

            Assert.Equal("choice", rows[0].GetProperty("type").GetString());
            Assert.Equal(0, rows[0].GetProperty("lines").GetArrayLength());
            Assert.Equal("B", rows[0].GetProperty("mostChosenWrongAnswer").GetString());
            var matching = rows[1];
            Assert.Equal("matching", matching.GetProperty("type").GetString());
            // Twelve points to win in three plays; six were won.
            Assert.Equal(12, matching.GetProperty("answerCount").GetInt32());
            Assert.Equal(6, matching.GetProperty("correctCount").GetInt32());
            Assert.Equal(JsonValueKind.Null, matching.GetProperty("mostChosenWrongAnswer").ValueKind);
            var lines = matching.GetProperty("lines").EnumerateArray().ToArray();
            Assert.Equal(["Judita", "Planine", "Dundo Maroje", "Osman"], lines.Select(line => line.GetProperty("label").GetString()));
            Assert.Equal([3, 3, 3, 3], lines.Select(line => line.GetProperty("answerCount").GetInt32()));
            Assert.Equal([1, 3, 1, 1], lines.Select(line => line.GetProperty("correctCount").GetInt32()));
            Assert.Equal(["Hanibal Lucić", null, "Ivan Gundulić", "Marin Držić"], lines.Select(line => line.GetProperty("mostChosenWrongAnswer").GetString()));
            Assert.Equal([2, 0, 2, 2], lines.Select(line => line.GetProperty("mostChosenWrongCount").GetInt32()));
        }

        [Fact]
        public async Task A_repeated_submission_key_returns_the_stored_rows_once()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var quiz = await Created(teacher, Matching());
            var body = new { quizId = Id(quiz), submissionKey = Guid.NewGuid(), answers = new[] { Matches(QuestionIds(quiz)[0], 1, 2, 4, 3) } };

            var first = await student.PostAsJsonAsync("/api/quiz/submit", body);
            var again = await student.PostAsJsonAsync("/api/quiz/submit", body);

            Assert.Equal(HttpStatusCode.Created, first.StatusCode);
            Assert.Equal(HttpStatusCode.Created, again.StatusCode);
            var repeated = await again.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(Id(await first.Content.ReadFromJsonAsync<JsonElement>()), Id(repeated));
            Assert.Equal(4, repeated.GetProperty("answers").GetArrayLength());
            Assert.Equal(1, (await Analytics(teacher, Id(quiz))).GetProperty("games").GetArrayLength());
        }

        [Fact]
        public async Task A_creators_play_of_their_own_matching_question_stores_no_rows()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var quiz = await Created(teacher, Matching());

            var response = await Submit(teacher, Id(quiz), Matches(QuestionIds(quiz)[0], 1, 2, 3, 4));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal(0, (await Analytics(teacher, Id(quiz))).GetProperty("games").GetArrayLength());
        }
    }
}
