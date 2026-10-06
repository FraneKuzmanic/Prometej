using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using static Prometej_tests.TestQuiz;

namespace Prometej_tests
{
    // A test as a quiz: its settings, who may read its questions, and what its creator does with it.
    public class TestModeTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        [Fact]
        public async Task A_test_is_a_private_quiz()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);

            var created = await Create(teacher, Header(isPrivate: false), Choice());
            var test = await CreateTest(teacher);
            var updated = await UpdateHeader(teacher, Header(test.Id, isPrivate: false));

            Assert.Equal(HttpStatusCode.BadRequest, created.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, updated.StatusCode);
            Assert.True((await Get(teacher, test.Id)).GetProperty("isTest").GetBoolean());
        }

        [Fact]
        public async Task A_test_keeps_its_time_limit_and_closing_time_and_another_quiz_keeps_no_closing_time()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var closesAt = new DateTimeOffset(2030, 5, 20, 9, 30, 0, TimeSpan.FromHours(2));

            var test = await CreateTest(teacher, timeLimitMinutes: 45, closesAt: closesAt);
            var practice = await Stored(teacher, await Create(teacher, Header(isTest: false, timeLimitMinutes: 30, closesAt: closesAt), Choice()));

            var storedTest = await Get(teacher, test.Id);
            Assert.Equal(45, storedTest.GetProperty("timeLimitMinutes").GetInt32());
            Assert.Equal(closesAt.UtcDateTime, storedTest.GetProperty("closesAt").GetDateTime());
            var storedPractice = await Get(teacher, practice.Id);
            Assert.False(storedPractice.GetProperty("isTest").GetBoolean());
            Assert.Equal(30, storedPractice.GetProperty("timeLimitMinutes").GetInt32());
            Assert.Equal(JsonValueKind.Null, storedPractice.GetProperty("closesAt").ValueKind);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(301)]
        [InlineData(-1)]
        public async Task A_time_limit_outside_one_to_three_hundred_is_refused(int minutes)
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var test = await CreateTest(teacher);

            var created = await Create(teacher, Header(timeLimitMinutes: minutes), Choice());
            var updated = await UpdateHeader(teacher, Header(test.Id, timeLimitMinutes: minutes));

            Assert.Equal(HttpStatusCode.BadRequest, created.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, updated.StatusCode);
        }

        [Fact]
        public async Task A_tests_questions_reach_only_its_creator_and_an_admin()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var admin = await factory.LoginAs(ApiFactory.AdminEmail);
            var student = await factory.LoginAsNewStudent();
            var nobody = factory.CreateHttpsClient();
            var test = await CreateTest(teacher, [Choice(), Matching()]);

            JsonElement[] withheld =
            [
                await student.GetFromJsonAsync<JsonElement>($"/api/quiz/get/{test.Id}?code={test.Code}"),
                await nobody.GetFromJsonAsync<JsonElement>($"/api/quiz/get/{test.Id}?code={test.Code}"),
                await student.GetFromJsonAsync<JsonElement>($"/api/quiz/getByCode/{test.Code}"),
                await nobody.GetFromJsonAsync<JsonElement>($"/api/quiz/getByCode/{test.Code}"),
                // The action knows no caller, so its creator gets the header there too.
                await teacher.GetFromJsonAsync<JsonElement>($"/api/quiz/getByCode/{test.Code}"),
            ];

            Assert.All(withheld, quiz =>
            {
                Assert.True(quiz.GetProperty("isTest").GetBoolean());
                Assert.Equal(0, quiz.GetProperty("questions").GetArrayLength());
                Assert.Equal(0, quiz.GetProperty("sourceTexts").GetArrayLength());
                Assert.DoesNotContain("correctOption", quiz.GetRawText());
            });
            Assert.Equal(2, (await Get(teacher, test.Id)).GetProperty("questions").GetArrayLength());
            Assert.Equal(2, (await Get(admin, test.Id)).GetProperty("questions").GetArrayLength());
        }

        [Fact]
        public async Task A_test_cannot_be_played_through_submit()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var test = await CreateTest(teacher, [Choice()]);

            var response = await student.PostAsJsonAsync("/api/quiz/submit",
                new { quizId = test.Id, answers = new[] { new { questionId = test.QuestionIds[0], chosenOption = 1 } } });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Empty(await Games(teacher, test.Id));
        }

        [Fact]
        public async Task A_header_sent_without_the_test_fields_makes_a_test_a_practice_quiz_again()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var test = await CreateTest(teacher, timeLimitMinutes: 20, closesAt: DateTimeOffset.UtcNow.AddDays(1));

            var response = await UpdateHeader(teacher, new { id = test.Id, title = "Kviz", isPrivate = true });

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            var quiz = await student.GetFromJsonAsync<JsonElement>($"/api/quiz/get/{test.Id}?code={test.Code}");
            Assert.False(quiz.GetProperty("isTest").GetBoolean());
            Assert.Equal(JsonValueKind.Null, quiz.GetProperty("timeLimitMinutes").ValueKind);
            Assert.Equal(JsonValueKind.Null, quiz.GetProperty("closesAt").ValueKind);
            Assert.Equal(3, quiz.GetProperty("questions").GetArrayLength());
        }

        [Fact]
        public async Task The_sixty_first_request_with_a_code_in_a_minute_is_refused_and_one_without_a_code_is_not()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var other = await factory.LoginAsNewStudent();
            var test = await CreateTest(teacher);
            var open = await Stored(teacher, await Create(teacher, Header(isPrivate: false, isTest: false), Choice()));

            var guesses = new List<HttpStatusCode>();
            for (var guess = 1; guess <= 60; guess++)
            {
                guesses.Add((await Info(student, test.Id, guess)).StatusCode);
            }
            var next = await Info(student, test.Id, test.Code);

            Assert.All(guesses, status => Assert.Equal(HttpStatusCode.NotFound, status));
            Assert.Equal(HttpStatusCode.TooManyRequests, next.StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await student.GetAsync("/api/quiz/search")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await student.GetAsync($"/api/quiz/get/{open.Id}")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await Info(other, test.Id, test.Code)).StatusCode);
        }

        private static Task<HttpResponseMessage> Close(HttpClient client, int quizId) =>
            client.PostAsync($"/api/quiz/close/{quizId}", null);

        private static Task<HttpResponseMessage> Reset(HttpClient client, int sittingId) =>
            client.PostAsync($"/api/sitting/{sittingId}/reset", null);

        private static async Task<JsonElement[]> Sittings(HttpClient client, int quizId) =>
            (await Analytics(client, quizId)).GetProperty("sittings").EnumerateArray().ToArray();

        [Fact]
        public async Task The_analytics_of_a_test_list_who_started_and_how_each_sitting_stands()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var done = await factory.LoginAsNewStudent();
            var late = await factory.LoginAsNewStudent();
            var working = await factory.LoginAsNewStudent();
            var test = await CreateTest(teacher, [Choice()], timeLimitMinutes: 10);
            var doneSitting = await Started(done, test);
            await Saved(done, Id(doneSitting), test.QuestionIds[0], 1);
            await Finished(done, Id(doneSitting));
            factory.Backdate(Id(await Started(late, test)), TimeSpan.FromMinutes(11));
            await Started(working, test);

            var analytics = await Analytics(teacher, test.Id);
            var sittings = analytics.GetProperty("sittings").EnumerateArray().ToArray();

            Assert.True(analytics.GetProperty("isTest").GetBoolean());
            Assert.Equal(test.Code, analytics.GetProperty("entryCode").GetInt32());
            Assert.Equal(10, analytics.GetProperty("timeLimitMinutes").GetInt32());
            Assert.False(analytics.GetProperty("isClosed").GetBoolean());
            // Newest start first; the backdated one started eleven minutes before the others.
            Assert.Equal(["running", "submitted", "expired"], sittings.Select(s => s.GetProperty("state").GetString()));
            Assert.All(sittings, sitting => Assert.Equal("Sara Student", sitting.GetProperty("userName").GetString()));
            Assert.Equal(JsonValueKind.Null, sittings[0].GetProperty("score").ValueKind);
            Assert.Equal(1, sittings[1].GetProperty("score").GetInt32());
            Assert.Equal(1, sittings[1].GetProperty("maxScore").GetInt32());
            Assert.Equal(0, sittings[2].GetProperty("score").GetInt32());
            Assert.All(analytics.GetProperty("games").EnumerateArray(), game => Assert.Equal("test", game.GetProperty("playedAs").GetString()));
            // A quiz that is not a test lists none.
            var practice = await Stored(teacher, await Create(teacher, Header(isTest: false), Choice()));
            Assert.Empty(await Sittings(teacher, practice.Id));
        }

        [Fact]
        public async Task Closing_a_test_ends_its_running_sittings_and_opens_the_reviews()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var newcomer = await factory.LoginAsNewStudent();
            var test = await CreateTest(teacher, [Choice(), Choice("Drugo")]);
            var sitting = await Started(student, test);
            await Saved(student, Id(sitting), test.QuestionIds[0], 1);

            var closed = await Close(teacher, test.Id);
            var again = await Close(teacher, test.Id);

            Assert.Equal(HttpStatusCode.NoContent, closed.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, again.StatusCode);
            var row = Assert.Single(await Sittings(teacher, test.Id));
            Assert.Equal("expired", row.GetProperty("state").GetString());
            Assert.Equal(1, row.GetProperty("score").GetInt32());
            Assert.Equal(2, row.GetProperty("maxScore").GetInt32());
            Assert.True((await Analytics(teacher, test.Id)).GetProperty("isClosed").GetBoolean());
            var review = await student.GetAsync($"/api/quiz/getGame/{row.GetProperty("quizGameId").GetInt32()}");
            Assert.Equal(HttpStatusCode.OK, review.StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await Start(newcomer, test.Id, test.Code)).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await Save(student, Id(sitting), test.QuestionIds[1], 1)).StatusCode);
        }

        [Fact]
        public async Task Only_a_test_is_closed()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var practice = await Stored(teacher, await Create(teacher, Header(isTest: false), Choice()));

            Assert.Equal(HttpStatusCode.BadRequest, (await Close(teacher, practice.Id)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await Close(teacher, 999999)).StatusCode);
        }

        [Fact]
        public async Task Only_the_creator_or_an_admin_closes_a_test_or_allows_a_second_sitting()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var otherTeacher = await factory.LoginAs(ApiFactory.OtherTeacherEmail);
            var admin = await factory.LoginAs(ApiFactory.AdminEmail);
            var student = await factory.LoginAsNewStudent();
            var test = await CreateTest(teacher, [Choice()]);
            var sitting = await Started(student, test);

            HttpResponseMessage[] refused =
            [
                await Close(otherTeacher, test.Id),
                await Reset(otherTeacher, Id(sitting)),
                await Close(student, test.Id),
                await Reset(student, Id(sitting)),
            ];
            var nobody = await Reset(factory.CreateHttpsClient(), Id(sitting));
            var reset = await Reset(admin, Id(sitting));
            var closed = await Close(admin, test.Id);

            Assert.All(refused, response => Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode));
            Assert.Equal(HttpStatusCode.Unauthorized, nobody.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, closed.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await Reset(admin, Id(sitting))).StatusCode);
        }

        [Fact]
        public async Task After_a_reset_the_student_sits_the_test_again_and_the_first_result_is_gone()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var test = await CreateTest(teacher, [Choice()]);
            var first = await Started(student, test);
            await Saved(student, Id(first), test.QuestionIds[0], 2);
            await Finished(student, Id(first));

            var reset = await Reset(teacher, Id(first));

            Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);
            Assert.Empty(await Games(teacher, test.Id));
            Assert.Empty(await Sittings(teacher, test.Id));
            Assert.Empty((await student.GetFromJsonAsync<JsonElement>("/api/quiz/getMyGames")).GetProperty("games").EnumerateArray());
            var second = await Started(student, test);
            await Saved(student, Id(second), test.QuestionIds[0], 1);
            Assert.Equal(1, (await Finished(student, Id(second))).GetProperty("score").GetInt32());
            Assert.Single(await Games(teacher, test.Id));
        }

        [Fact]
        public async Task A_tests_questions_are_locked_from_its_first_sitting()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var test = await CreateTest(teacher, [Choice()], timeLimitMinutes: 10);
            Assert.False((await Get(teacher, test.Id)).GetProperty("questionsLocked").GetBoolean());
            var sitting = await Started(student, test);

            var questions = await teacher.PutAsJsonAsync("/api/quiz/update",
                new { quiz = Header(test.Id, timeLimitMinutes: 10), questions = new[] { Choice("Izmijenjeno", test.QuestionIds[0]) } });
            var header = await UpdateHeader(teacher, Header(test.Id, timeLimitMinutes: 30, title: "Novi naslov"));

            Assert.Equal(HttpStatusCode.Conflict, questions.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, header.StatusCode);
            var stored = await Get(teacher, test.Id);
            Assert.True(stored.GetProperty("questionsLocked").GetBoolean());
            Assert.Equal("Novi naslov", stored.GetProperty("title").GetString());
            Assert.Equal("Izbor", stored.GetProperty("questions")[0].GetProperty("questionTitle").GetString());
            // The sitting keeps the limit it started with.
            var resumed = await (await Start(student, test.Id, test.Code)).Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(sitting.GetProperty("endsAt").GetDateTime(), resumed.GetProperty("endsAt").GetDateTime());
            // The lock is told to whoever may edit, and to nobody else.
            Assert.False((await student.GetFromJsonAsync<JsonElement>($"/api/quiz/get/{test.Id}?code={test.Code}")).GetProperty("questionsLocked").GetBoolean());
        }

        [Fact]
        public async Task A_quizs_mode_cannot_change_once_it_has_results()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var test = await CreateTest(teacher, [Choice()]);
            await Started(student, test);
            var practice = await Stored(teacher, await Create(teacher, Header(isTest: false), Choice()));
            var played = await student.PostAsJsonAsync("/api/quiz/submit",
                new { quizId = practice.Id, answers = new[] { new { questionId = practice.QuestionIds[0], chosenOption = 1 } } });

            var toPractice = await UpdateHeader(teacher, Header(test.Id, isTest: false));
            var toTest = await UpdateHeader(teacher, Header(practice.Id));

            Assert.Equal(HttpStatusCode.Created, played.StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, toPractice.StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, toTest.StatusCode);
            Assert.True((await Get(teacher, test.Id)).GetProperty("isTest").GetBoolean());
            Assert.False((await Get(teacher, practice.Id)).GetProperty("isTest").GetBoolean());
        }

        [Fact]
        public async Task A_closing_time_moved_later_reopens_a_test()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var test = await CreateTest(teacher, [Choice()]);
            await Close(teacher, test.Id);
            Assert.Equal(HttpStatusCode.Conflict, (await Start(student, test.Id, test.Code)).StatusCode);

            var reopened = await UpdateHeader(teacher, Header(test.Id, closesAt: DateTimeOffset.UtcNow.AddHours(1)));

            Assert.Equal(HttpStatusCode.NoContent, reopened.StatusCode);
            Assert.False((await InfoOf(student, test)).GetProperty("isClosed").GetBoolean());
            Assert.Equal(HttpStatusCode.Created, (await Start(student, test.Id, test.Code)).StatusCode);
        }
    }
}
