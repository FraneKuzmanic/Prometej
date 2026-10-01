using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prometej_persistance;

namespace Prometej_tests
{
    public class RetiredQuestionTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        private record CreatedQuiz(int Id, int[] QuestionIds);

        private static object Question(string title, int id = 0) => new
        {
            id,
            questionTitle = title,
            firstAnswer = "A",
            secondAnswer = "B",
            thirdAnswer = "C",
            fourthAnswer = "D",
            correctOption = 1,
        };

        private static async Task<int[]> QuestionIds(HttpClient client, int quizId) =>
            (await client.GetFromJsonAsync<JsonElement>($"/api/quiz/get/{quizId}")).GetProperty("questions")
                .EnumerateArray().Select(q => q.GetProperty("id").GetInt32()).ToArray();

        // Two questions, "Prvo" and "Drugo", in that order.
        private static async Task<CreatedQuiz> CreateQuiz(HttpClient client)
        {
            var body = new
            {
                quiz = new { title = "Kviz", isPrivate = false },
                questions = new[] { Question("Prvo"), Question("Drugo") },
            };
            var response = await client.PostAsJsonAsync("/api/quiz/create", body);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var id = await response.Content.ReadFromJsonAsync<int>();
            return new CreatedQuiz(id, await QuestionIds(client, id));
        }

        private static Task<HttpResponseMessage> Update(HttpClient client, int quizId, params object[] questions) =>
            client.PutAsJsonAsync("/api/quiz/update", new
            {
                quiz = new { id = quizId, title = "Kviz", isPrivate = false },
                questions = questions.Length > 0 ? questions : null,
            });

        private static Task<HttpResponseMessage> Submit(HttpClient client, int quizId, params int[] questionIds) =>
            client.PostAsJsonAsync("/api/quiz/submit", new
            {
                quizId,
                answers = questionIds.Select(questionId => new { questionId, chosenOption = 1 }),
            });

        private static async Task<JsonElement[]> Analytics(HttpClient client, int quizId) =>
            (await client.GetFromJsonAsync<JsonElement>($"/api/quiz/getAnalytics/{quizId}")).EnumerateArray().ToArray();

        // null: the row is gone. Otherwise whether the stored question is retired.
        private bool? IsRetired(int questionId)
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();
            return context.Questions.AsNoTracking().Where(q => q.Id == questionId)
                .Select(q => (bool?)q.IsRetired).FirstOrDefault();
        }

        [Fact]
        public async Task A_removed_question_nobody_answered_is_deleted()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var quiz = await CreateQuiz(teacher);

            var response = await Update(teacher, quiz.Id, Question("Prvo", quiz.QuestionIds[0]));

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal([quiz.QuestionIds[0]], await QuestionIds(teacher, quiz.Id));
            Assert.Null(IsRetired(quiz.QuestionIds[1]));
        }

        [Fact]
        public async Task A_removed_question_that_was_answered_is_retired_and_the_old_quiz_game_keeps_it()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var quiz = await CreateQuiz(teacher);
            await Submit(student, quiz.Id, quiz.QuestionIds);

            var response = await Update(teacher, quiz.Id, Question("Prvo", quiz.QuestionIds[0]));

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal([quiz.QuestionIds[0]], await QuestionIds(teacher, quiz.Id));
            Assert.True(IsRetired(quiz.QuestionIds[1]));
            var oldGame = Assert.Single(await Analytics(teacher, quiz.Id));
            Assert.Equal(2, oldGame.GetProperty("score").GetInt32());
            Assert.Equal(2, oldGame.GetProperty("answers").GetArrayLength());

            // A new play is of the one question that is left.
            var withRetired = await Submit(student, quiz.Id, quiz.QuestionIds);
            var withoutRetired = await Submit(student, quiz.Id, quiz.QuestionIds[0]);

            Assert.Equal(HttpStatusCode.BadRequest, withRetired.StatusCode);
            Assert.Equal(HttpStatusCode.Created, withoutRetired.StatusCode);
            Assert.Equal([1, 2], (await Analytics(teacher, quiz.Id)).Select(g => g.GetProperty("score").GetInt32()));
        }

        [Fact]
        public async Task A_retired_question_cannot_be_edited_or_brought_back()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var quiz = await CreateQuiz(teacher);
            await Submit(student, quiz.Id, quiz.QuestionIds);
            await Update(teacher, quiz.Id, Question("Prvo", quiz.QuestionIds[0]));

            var response = await Update(teacher, quiz.Id,
                Question("Izmijenjeno", quiz.QuestionIds[0]), Question("Vraćeno", quiz.QuestionIds[1]));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            var stored = await teacher.GetFromJsonAsync<JsonElement>($"/api/quiz/get/{quiz.Id}");
            var question = Assert.Single(stored.GetProperty("questions").EnumerateArray());
            Assert.Equal("Prvo", question.GetProperty("questionTitle").GetString());
            Assert.True(IsRetired(quiz.QuestionIds[1]));
        }

        [Fact]
        public async Task An_update_without_a_question_list_removes_nothing()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var quiz = await CreateQuiz(teacher);

            var response = await Update(teacher, quiz.Id);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(quiz.QuestionIds, await QuestionIds(teacher, quiz.Id));
        }

        [Fact]
        public async Task A_question_added_in_an_edit_comes_after_the_ones_that_stay()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var quiz = await CreateQuiz(teacher);

            await Update(teacher, quiz.Id, Question("Treće"), Question("Drugo", quiz.QuestionIds[1]));

            var stored = await teacher.GetFromJsonAsync<JsonElement>($"/api/quiz/get/{quiz.Id}");
            Assert.Equal(["Drugo", "Treće"],
                stored.GetProperty("questions").EnumerateArray().Select(q => q.GetProperty("questionTitle").GetString()));
        }

        [Fact]
        public async Task A_quiz_with_a_retired_question_can_be_deleted()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var quiz = await CreateQuiz(teacher);
            await Submit(student, quiz.Id, quiz.QuestionIds);
            await Update(teacher, quiz.Id, Question("Prvo", quiz.QuestionIds[0]));

            var response = await teacher.DeleteAsync($"/api/quiz/delete/{quiz.Id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Null(IsRetired(quiz.QuestionIds[1]));
        }
    }
}
