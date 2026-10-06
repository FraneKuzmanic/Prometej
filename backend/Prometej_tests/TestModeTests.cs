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
    }
}
