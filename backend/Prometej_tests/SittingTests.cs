using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using static Prometej_tests.TestQuiz;

namespace Prometej_tests
{
    // A sitting as its user goes through it: start, answers, the end and the result.
    public class SittingTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        private static string[] AnswerTexts(JsonElement game) =>
            game.GetProperty("answers").EnumerateArray().Select(answer => answer.GetProperty("answerText").GetString()!).ToArray();

        private const int Realizam = 4;

        private static Task<HttpResponseMessage> Discard(HttpClient client, int sittingId) =>
            client.DeleteAsync($"/api/sitting/{sittingId}");

        // A public quiz, which anyone signed in may sit as a mock, without a code.
        private static async Task<Created> CreateListed(HttpClient teacher, object[]? questions = null, int? timeLimitMinutes = null) =>
            await Stored(teacher, await teacher.PostAsJsonAsync("/api/quiz/create", new
            {
                quiz = new { title = "Javni kviz", isPrivate = false, periodId = Realizam, timeLimitMinutes },
                questions = questions ?? OneOfEach,
            }));

        private static async Task<JsonElement> StartedMock(HttpClient client, int quizId)
        {
            var response = await Start(client, quizId, null);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }

        [Fact]
        public async Task A_sitting_needs_a_session()
        {
            var nobody = factory.CreateHttpsClient();

            HttpResponseMessage[] responses =
            [
                await Info(nobody, 1, 12345),
                await Start(nobody, 1, 12345),
                await Save(nobody, 1, 1, 1),
                await Finish(nobody, 1),
                await Discard(nobody, 1),
                await nobody.PostAsync("/api/sitting/1/reset", null),
            ];

            Assert.All(responses, response => Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode));
        }

        [Fact]
        public async Task A_test_is_not_found_without_its_code()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var test = await CreateTest(teacher);
            var otherTest = await CreateTest(teacher);
            var practice = await Stored(teacher, await Create(teacher, Header(isTest: false), Choice()));

            HttpResponseMessage[] responses =
            [
                await Info(student, test.Id, null),
                await Info(student, test.Id, otherTest.Code),
                await Start(student, test.Id, null),
                await Start(student, test.Id, otherTest.Code),
                // A private quiz that is not a test is practice: there is nothing to sit.
                await Info(student, practice.Id, practice.Code),
                await Start(student, practice.Id, practice.Code),
                await Info(student, 999999, test.Code),
            ];

            Assert.All(responses, response => Assert.Equal(HttpStatusCode.NotFound, response.StatusCode));
        }

        [Fact]
        public async Task The_start_screen_says_what_the_test_is()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var closesAt = DateTimeOffset.UtcNow.AddDays(2);
            var test = await CreateTest(teacher, timeLimitMinutes: 45, closesAt: closesAt);

            var info = await InfoOf(student, test);

            Assert.Equal("Provjera", info.GetProperty("title").GetString());
            Assert.True(info.GetProperty("isTest").GetBoolean());
            Assert.Equal(3, info.GetProperty("questionCount").GetInt32());
            // One for the choice, four pairs, five places.
            Assert.Equal(10, info.GetProperty("maxScore").GetInt32());
            Assert.Equal(45, info.GetProperty("timeLimitMinutes").GetInt32());
            Assert.Equal(closesAt.UtcDateTime, info.GetProperty("closesAt").GetDateTime(), TimeSpan.FromMilliseconds(1));
            Assert.False(info.GetProperty("isClosed").GetBoolean());
            Assert.False(info.GetProperty("isOwn").GetBoolean());
            Assert.False(info.GetProperty("running").GetBoolean());
            Assert.Equal(JsonValueKind.Null, info.GetProperty("result").ValueKind);
            Assert.True((await InfoOf(teacher, test)).GetProperty("isOwn").GetBoolean());
        }

        [Fact]
        public async Task A_sitting_sends_the_questions_without_their_answers()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var test = await CreateTest(teacher);

            var response = await Start(student, test.Id, test.Code);
            var body = await response.Content.ReadAsStringAsync();
            var sitting = JsonDocument.Parse(body).RootElement;

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.All(new[] { "correctOption", "content", "hintText", "exploreMore", "pairs", "extras", "shown" },
                name => Assert.DoesNotContain($"\"{name}\"", body, StringComparison.OrdinalIgnoreCase));
            Assert.Equal(test.QuestionIds, sitting.GetProperty("questions").EnumerateArray().Select(q => q.GetProperty("questionId").GetInt32()));
            Assert.Equal(["A", "B", "C", "D"], Texts(Question(sitting, test.QuestionIds[0]), "options"));
            var matching = Question(sitting, test.QuestionIds[1]);
            Assert.Equal(Pairs.Select(pair => pair.Left), Texts(matching, "lefts"));
            Assert.Equal(Pairs.Select(pair => pair.Right).Append(Extra).Order(), Texts(matching, "rights").Order());
            // In the stored order the pairs would read straight across.
            Assert.NotEqual(Pairs.Select(pair => pair.Right).Append(Extra), Texts(matching, "rights"));
            var items = Texts(Question(sitting, test.QuestionIds[2]), "items");
            Assert.Equal(Items.Order(), items.Order());
            Assert.NotEqual(Items, items);
        }

        [Fact]
        public async Task Starting_again_resumes_the_running_sitting_with_its_saved_answers()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var test = await CreateTest(teacher);
            var sitting = await Started(student, test);
            await Saved(student, Id(sitting), test.QuestionIds[0], 3);

            var again = await Start(student, test.Id, test.Code);
            var resumed = await again.Content.ReadFromJsonAsync<JsonElement>();

            Assert.Equal(HttpStatusCode.OK, again.StatusCode);
            Assert.Equal(Id(sitting), Id(resumed));
            Assert.Equal([3], Question(resumed, test.QuestionIds[0]).GetProperty("given").EnumerateArray().Select(n => n.GetInt32()));
            Assert.Equal(Texts(Question(sitting, test.QuestionIds[2]), "items"), Texts(Question(resumed, test.QuestionIds[2]), "items"));
            Assert.True((await InfoOf(student, test)).GetProperty("running").GetBoolean());
        }

        [Fact]
        public async Task A_matching_answer_is_read_through_the_order_it_was_shown_in()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var right = await factory.LoginAsNewStudent();
            var crossed = await factory.LoginAsNewStudent();
            var test = await CreateTest(teacher, [Matching()]);
            var rights = Pairs.Select(pair => pair.Right).ToArray();

            var first = await Started(right, test);
            await Saved(right, Id(first), test.QuestionIds[0], ShownNumbers(first.GetProperty("questions")[0], "rights", rights));
            var allRight = await Finished(right, Id(first));
            // The first two works get each other's author.
            var second = await Started(crossed, test);
            await Saved(crossed, Id(second), test.QuestionIds[0],
                ShownNumbers(second.GetProperty("questions")[0], "rights", [rights[1], rights[0], rights[2], rights[3]]));
            var twoWrong = await Finished(crossed, Id(second));

            Assert.Equal(4, allRight.GetProperty("score").GetInt32());
            Assert.Equal(4, allRight.GetProperty("maxScore").GetInt32());
            Assert.Equal(2, twoWrong.GetProperty("score").GetInt32());
            var games = await Games(teacher, test.Id);
            Assert.Contains(games, game => AnswerTexts(game).SequenceEqual(rights));
            Assert.Contains(games, game => AnswerTexts(game).SequenceEqual([rights[1], rights[0], rights[2], rights[3]]));
        }

        [Fact]
        public async Task An_ordering_answer_is_read_through_the_order_it_was_shown_in()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var right = await factory.LoginAsNewStudent();
            var swapped = await factory.LoginAsNewStudent();
            var test = await CreateTest(teacher, [Ordering()]);
            string[] lastTwoSwapped = [Items[0], Items[1], Items[2], Items[4], Items[3]];

            var first = await Started(right, test);
            await Saved(right, Id(first), test.QuestionIds[0], ShownNumbers(first.GetProperty("questions")[0], "items", Items));
            var allRight = await Finished(right, Id(first));
            var second = await Started(swapped, test);
            await Saved(swapped, Id(second), test.QuestionIds[0], ShownNumbers(second.GetProperty("questions")[0], "items", lastTwoSwapped));
            var twoWrong = await Finished(swapped, Id(second));

            Assert.Equal(5, allRight.GetProperty("score").GetInt32());
            Assert.Equal(3, twoWrong.GetProperty("score").GetInt32());
            Assert.Contains(await Games(teacher, test.Id), game => AnswerTexts(game).SequenceEqual(lastTwoSwapped));
        }

        [Fact]
        public async Task An_unanswered_point_is_a_wrong_row_with_no_answer()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var test = await CreateTest(teacher);
            var sitting = await Started(student, test);
            var rights = Pairs.Select(pair => pair.Right).ToArray();
            var given = ShownNumbers(Question(sitting, test.QuestionIds[1]), "rights", rights);
            given[1] = 0;

            // The choice and the ordering question are never answered, and one pair is left open.
            await Saved(student, Id(sitting), test.QuestionIds[1], given);
            var result = await Finished(student, Id(sitting));

            Assert.Equal(3, result.GetProperty("score").GetInt32());
            Assert.Equal(10, result.GetProperty("maxScore").GetInt32());
            var analytics = await Analytics(teacher, test.Id);
            var game = Assert.Single(analytics.GetProperty("games").EnumerateArray());
            Assert.Equal(["", rights[0], "", rights[2], rights[3], "", "", "", "", ""], AnswerTexts(game));
            Assert.Equal(3, game.GetProperty("score").GetInt32());
            // Nobody chose the empty answer, so it is not the wrong answer chosen most often.
            var choice = analytics.GetProperty("questions")[0];
            Assert.Equal(0, choice.GetProperty("correctCount").GetInt32());
            Assert.Equal(JsonValueKind.Null, choice.GetProperty("mostChosenWrongAnswer").ValueKind);
        }

        public static TheoryData<int, int[]> WrongShapes => new()
        {
            { 0, [1, 2] },
            { 0, [5] },
            { 0, [-1] },
            { 0, [] },
            { 1, [1, 2, 3] },
            { 1, [1, 1, 2, 3] },
            { 1, [1, 2, 3, 6] },
            { 2, [1, 2, 3, 4, 5, 6] },
            { 2, [1, 2, 3, 4, 4] },
        };

        [Theory]
        [MemberData(nameof(WrongShapes))]
        public async Task An_answer_of_the_wrong_shape_is_refused(int question, int[] given)
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var test = await CreateTest(teacher);
            var sitting = await Started(student, test);

            var response = await Save(student, Id(sitting), test.QuestionIds[question], given);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task An_answer_to_a_question_of_another_quiz_is_not_found()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var test = await CreateTest(teacher);
            var otherTest = await CreateTest(teacher);
            var sitting = await Started(student, test);

            var response = await Save(student, Id(sitting), otherTest.QuestionIds[0], 1);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task An_answer_can_be_changed_until_the_sitting_ends_and_not_after()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var test = await CreateTest(teacher, [Choice()]);
            var sitting = await Started(student, test);

            await Saved(student, Id(sitting), test.QuestionIds[0], 2);
            await Saved(student, Id(sitting), test.QuestionIds[0], 1);
            var result = await Finished(student, Id(sitting));
            var late = await Save(student, Id(sitting), test.QuestionIds[0], 2);

            Assert.Equal(1, result.GetProperty("score").GetInt32());
            Assert.Equal("submitted", result.GetProperty("outcome").GetString());
            Assert.Equal(HttpStatusCode.Conflict, late.StatusCode);
            Assert.Equal(["A"], AnswerTexts(Assert.Single(await Games(teacher, test.Id))));
        }

        [Fact]
        public async Task Finishing_twice_gives_the_same_result()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var test = await CreateTest(teacher, [Choice()]);
            var sitting = await Started(student, test);
            await Saved(student, Id(sitting), test.QuestionIds[0], 1);

            var first = await Finished(student, Id(sitting));
            var second = await Finished(student, Id(sitting));

            Assert.Equal(first.GetProperty("gameId").GetInt32(), second.GetProperty("gameId").GetInt32());
            Assert.Equal(1, second.GetProperty("score").GetInt32());
            Assert.Single(await Games(teacher, test.Id));
        }

        [Fact]
        public async Task A_test_is_sat_once()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var test = await CreateTest(teacher, [Choice()]);
            var sitting = await Started(student, test);
            var result = await Finished(student, Id(sitting));

            var again = await Start(student, test.Id, test.Code);

            Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
            var info = await InfoOf(student, test);
            Assert.False(info.GetProperty("running").GetBoolean());
            Assert.Equal(result.GetProperty("gameId").GetInt32(), info.GetProperty("result").GetProperty("gameId").GetInt32());
            Assert.Single(await Games(teacher, test.Id));
        }

        [Fact]
        public async Task The_creator_cannot_sit_their_own_test()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var test = await CreateTest(teacher);

            var response = await Start(teacher, test.Id, test.Code);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Someone_elses_sitting_is_not_found()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var other = await factory.LoginAsNewStudent();
            var test = await CreateTest(teacher, [Choice()]);
            var sitting = await Started(student, test);

            HttpResponseMessage[] responses =
            [
                await Save(other, Id(sitting), test.QuestionIds[0], 1),
                await Finish(other, Id(sitting)),
                await Save(teacher, Id(sitting), test.QuestionIds[0], 1),
                await Finish(teacher, Id(sitting)),
            ];

            Assert.All(responses, response => Assert.Equal(HttpStatusCode.NotFound, response.StatusCode));
            Assert.True((await InfoOf(student, test)).GetProperty("running").GetBoolean());
        }

        [Fact]
        public async Task A_sitting_past_its_time_limit_is_ended_by_the_next_read()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var test = await CreateTest(teacher, [Choice(), Choice("Drugo")], timeLimitMinutes: 10);
            var sitting = await Started(student, test);
            var deadline = sitting.GetProperty("endsAt").GetDateTime() - TimeSpan.FromMinutes(11);
            await Saved(student, Id(sitting), test.QuestionIds[0], 1);

            factory.Backdate(Id(sitting), TimeSpan.FromMinutes(11));
            var late = await Save(student, Id(sitting), test.QuestionIds[1], 1);

            Assert.Equal(HttpStatusCode.Conflict, late.StatusCode);
            var game = Assert.Single(await Games(teacher, test.Id));
            Assert.Equal(["A", ""], AnswerTexts(game));
            Assert.Equal(1, game.GetProperty("score").GetInt32());
            // Dated when its time ran out, not when somebody noticed.
            Assert.Equal(deadline, game.GetProperty("datePlayed").GetDateTime());
            Assert.Equal("expired", (await InfoOf(student, test)).GetProperty("result").GetProperty("outcome").GetString());
        }

        [Fact]
        public async Task A_closing_time_ends_a_sitting_before_its_time_limit()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var test = await CreateTest(teacher, [Choice()], timeLimitMinutes: 60);
            var sitting = await Started(student, test);
            await Saved(student, Id(sitting), test.QuestionIds[0], 1);

            var closed = await UpdateHeader(teacher, Header(test.Id, timeLimitMinutes: 60, closesAt: DateTimeOffset.UtcNow.AddSeconds(-1)));
            var info = await InfoOf(student, test);

            Assert.Equal(HttpStatusCode.NoContent, closed.StatusCode);
            Assert.False(info.GetProperty("running").GetBoolean());
            Assert.True(info.GetProperty("isClosed").GetBoolean());
            Assert.Equal("expired", info.GetProperty("result").GetProperty("outcome").GetString());
            Assert.Equal(1, info.GetProperty("result").GetProperty("score").GetInt32());
        }

        [Fact]
        public async Task A_closed_test_cannot_be_started()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var test = await CreateTest(teacher, closesAt: DateTimeOffset.UtcNow.AddMinutes(-1));

            var response = await Start(student, test.Id, test.Code);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.True((await InfoOf(student, test)).GetProperty("isClosed").GetBoolean());
        }

        [Fact]
        public async Task The_result_of_a_test_carries_no_answers_and_its_review_waits_for_the_close()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var test = await CreateTest(teacher, [Choice()]);
            var sitting = await Started(student, test);
            await Saved(student, Id(sitting), test.QuestionIds[0], 2);

            var finished = await Finish(student, Id(sitting));
            var body = await finished.Content.ReadAsStringAsync();
            var gameId = JsonDocument.Parse(body).RootElement.GetProperty("gameId").GetInt32();
            var early = await student.GetAsync($"/api/quiz/getGame/{gameId}");
            var listed = (await student.GetFromJsonAsync<JsonElement>("/api/quiz/getMyGames")).GetProperty("games")[0];

            Assert.DoesNotContain("answers", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("correctAnswer", body, StringComparison.OrdinalIgnoreCase);
            Assert.False(JsonDocument.Parse(body).RootElement.GetProperty("reviewAvailable").GetBoolean());
            Assert.Equal(HttpStatusCode.Conflict, early.StatusCode);
            Assert.Equal("test", listed.GetProperty("playedAs").GetString());
            Assert.False(listed.GetProperty("reviewAvailable").GetBoolean());

            await UpdateHeader(teacher, Header(test.Id, closesAt: DateTimeOffset.UtcNow.AddSeconds(-1)));
            var review = await student.GetAsync($"/api/quiz/getGame/{gameId}");
            var reviewed = await review.Content.ReadFromJsonAsync<JsonElement>();

            Assert.Equal(HttpStatusCode.OK, review.StatusCode);
            Assert.Equal("test", reviewed.GetProperty("playedAs").GetString());
            Assert.Equal(["B"], AnswerTexts(reviewed));
            Assert.True((await student.GetFromJsonAsync<JsonElement>("/api/quiz/getMyGames")).GetProperty("games")[0].GetProperty("reviewAvailable").GetBoolean());
        }

        [Fact]
        public async Task Two_requests_ending_one_sitting_store_one_quiz_game()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var test = await CreateTest(teacher, timeLimitMinutes: 10);
            var sitting = await Started(student, test);
            await Saved(student, Id(sitting), test.QuestionIds[0], 1);
            factory.Backdate(Id(sitting), TimeSpan.FromMinutes(11));

            var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(i => i % 2 == 0
                ? Info(student, test.Id, test.Code)
                : teacher.GetAsync($"/api/quiz/getAnalytics/{test.Id}")));

            Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
            Assert.Single(await Games(teacher, test.Id));
        }

        [Fact]
        public async Task A_sittings_result_goes_with_its_users_account()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var finished = await factory.LoginAsNewStudent();
            var running = await factory.LoginAsNewStudent();
            var test = await CreateTest(teacher, [Choice()]);
            await Finished(finished, Id(await Started(finished, test)));
            await Started(running, test);

            var first = await finished.DeleteAsync("/api/user/me");
            var second = await running.DeleteAsync("/api/user/me");

            Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
            Assert.Empty(await Games(teacher, test.Id));
        }

        [Fact]
        public async Task A_listed_quiz_is_sat_as_often_as_wanted_and_each_sitting_is_a_quiz_game()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var quiz = await CreateListed(teacher, [Choice(), Choice("Drugo")]);

            var info = await (await Info(student, quiz.Id, null)).Content.ReadFromJsonAsync<JsonElement>();
            var first = await StartedMock(student, quiz.Id);
            await Saved(student, Id(first), quiz.QuestionIds[0], 1);
            var firstResult = await Finished(student, Id(first));
            var second = await StartedMock(student, quiz.Id);
            await Saved(student, Id(second), quiz.QuestionIds[0], 1);
            await Saved(student, Id(second), quiz.QuestionIds[1], 1);
            var secondResult = await Finished(student, Id(second));

            Assert.False(info.GetProperty("isTest").GetBoolean());
            Assert.Equal(JsonValueKind.Null, info.GetProperty("closesAt").ValueKind);
            Assert.DoesNotContain("correctOption", first.GetRawText());
            Assert.Equal(1, firstResult.GetProperty("score").GetInt32());
            Assert.True(firstResult.GetProperty("reviewAvailable").GetBoolean());
            Assert.Equal(2, secondResult.GetProperty("score").GetInt32());
            // A mock has no "the" result: the start screen offers another go.
            var after = await (await Info(student, quiz.Id, null)).Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(JsonValueKind.Null, after.GetProperty("result").ValueKind);
            Assert.False(after.GetProperty("running").GetBoolean());
            var mine = await student.GetFromJsonAsync<JsonElement>("/api/quiz/getMyGames");
            Assert.All(mine.GetProperty("games").EnumerateArray(), game =>
            {
                Assert.Equal("mock", game.GetProperty("playedAs").GetString());
                Assert.True(game.GetProperty("reviewAvailable").GetBoolean());
            });
            Assert.Equal(2, mine.GetProperty("games").GetArrayLength());
            // The review opens at once, and the play counts in the Period's progress.
            var review = await student.GetFromJsonAsync<JsonElement>($"/api/quiz/getGame/{firstResult.GetProperty("gameId").GetInt32()}");
            Assert.Equal("mock", review.GetProperty("playedAs").GetString());
            Assert.Equal(["A", ""], AnswerTexts(review));
            var progress = Assert.Single(mine.GetProperty("progress").EnumerateArray());
            Assert.Equal(Realizam, progress.GetProperty("periodId").GetInt32());
            Assert.Equal(100, progress.GetProperty("averageBestPercent").GetInt32());
            Assert.All(await Games(teacher, quiz.Id), game => Assert.Equal("mock", game.GetProperty("playedAs").GetString()));
            // The same quiz played as practice is labelled as that.
            await student.PostAsJsonAsync("/api/quiz/submit", new { quizId = quiz.Id, answers = quiz.QuestionIds.Select(questionId => new { questionId, chosenOption = 1 }) });
            Assert.Equal("practice", (await student.GetFromJsonAsync<JsonElement>("/api/quiz/getMyGames")).GetProperty("games")[0].GetProperty("playedAs").GetString());
        }

        [Fact]
        public async Task A_mock_sitting_has_one_running_at_a_time_and_can_be_discarded()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var other = await factory.LoginAsNewStudent();
            var quiz = await CreateListed(teacher, [Choice()]);
            var first = await StartedMock(student, quiz.Id);

            var again = await Start(student, quiz.Id, null);
            var notTheirs = await Discard(other, Id(first));
            var discarded = await Discard(student, Id(first));
            var gone = await Discard(student, Id(first));
            var second = await StartedMock(student, quiz.Id);

            Assert.Equal(HttpStatusCode.OK, again.StatusCode);
            Assert.Equal(Id(first), Id(await again.Content.ReadFromJsonAsync<JsonElement>()));
            Assert.Equal(HttpStatusCode.NotFound, notTheirs.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, discarded.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
            Assert.NotEqual(Id(first), Id(second));
            Assert.Empty(await Games(teacher, quiz.Id));
            // One that has ended is a result, and stays.
            await Finished(student, Id(second));
            Assert.Equal(HttpStatusCode.Conflict, (await Discard(student, Id(second))).StatusCode);
            Assert.Single(await Games(teacher, quiz.Id));
        }

        [Fact]
        public async Task A_private_practice_quiz_and_an_empty_quiz_cannot_be_sat()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var practice = await Stored(teacher, await Create(teacher, Header(isTest: false), Choice()));
            var teacherId = (await teacher.GetFromJsonAsync<JsonElement>("/api/user/me")).GetProperty("id").GetInt32();
            var empty = factory.AddLegacyQuiz(teacherId);

            HttpResponseMessage[] responses =
            [
                await Info(student, practice.Id, null),
                await Start(student, practice.Id, null),
                await Info(student, empty, null),
                await Start(student, empty, null),
            ];

            Assert.All(responses, response => Assert.Equal(HttpStatusCode.NotFound, response.StatusCode));
        }

        [Fact]
        public async Task A_tests_sitting_cannot_be_discarded()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var test = await CreateTest(teacher, [Choice()]);
            var sitting = await Started(student, test);

            var response = await Discard(student, Id(sitting));

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.True((await InfoOf(student, test)).GetProperty("running").GetBoolean());
        }

        [Fact]
        public async Task Editing_a_quizs_questions_discards_the_mock_sittings_running_on_it()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var quiz = await CreateListed(teacher, [Choice(), Matching()]);
            var sitting = await StartedMock(student, quiz.Id);
            object header = new { id = quiz.Id, title = "Novi naslov", isPrivate = false, periodId = Realizam };

            var headerOnly = await UpdateHeader(teacher, header);
            var kept = await Save(student, Id(sitting), quiz.QuestionIds[0], 1);
            // The matching question is removed while the sitting holds an order made for it.
            var edited = await teacher.PutAsJsonAsync("/api/quiz/update", new { quiz = header, questions = new[] { Choice("Izbor", quiz.QuestionIds[0]) } });
            var lost = await Save(student, Id(sitting), quiz.QuestionIds[0], 2);

            Assert.Equal(HttpStatusCode.NoContent, headerOnly.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, kept.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, edited.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, lost.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await Finish(student, Id(sitting))).StatusCode);
            Assert.Empty(await Games(teacher, quiz.Id));
            Assert.NotEqual(Id(sitting), Id(await StartedMock(student, quiz.Id)));
        }

        [Fact]
        public async Task A_mock_sitting_keeps_the_quizs_time_limit()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var quiz = await CreateListed(teacher, [Choice()], timeLimitMinutes: 20);

            var info = await (await Info(student, quiz.Id, null)).Content.ReadFromJsonAsync<JsonElement>();
            var sitting = await StartedMock(student, quiz.Id);
            await Saved(student, Id(sitting), quiz.QuestionIds[0], 1);
            factory.Backdate(Id(sitting), TimeSpan.FromMinutes(21));
            var late = await Save(student, Id(sitting), quiz.QuestionIds[0], 2);

            Assert.Equal(20, info.GetProperty("timeLimitMinutes").GetInt32());
            Assert.Equal(TimeSpan.FromMinutes(20), sitting.GetProperty("endsAt").GetDateTime() - sitting.GetProperty("startedAt").GetDateTime());
            Assert.Equal(HttpStatusCode.Conflict, late.StatusCode);
            var game = Assert.Single(await Games(teacher, quiz.Id));
            Assert.Equal("mock", game.GetProperty("playedAs").GetString());
            Assert.Equal(1, game.GetProperty("score").GetInt32());
        }
    }
}
