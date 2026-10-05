using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prometej_persistance;

namespace Prometej_tests
{
    public class SourceTextTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        private const string Caption = "A. G. Matoš, Jesenje veče";
        private const string Body = "Olovne i teške snove snivaju\nOblaci nad tamnim gorskim stranama";

        // sourceTextNo is the 1-based number of the question's passage in the sent list.
        private static object Question(string title, int? sourceTextNo = null, int id = 0) => new
        {
            id,
            questionTitle = title,
            firstAnswer = "A",
            secondAnswer = "B",
            thirdAnswer = "C",
            fourthAnswer = "D",
            correctOption = 1,
            sourceTextNo,
        };

        private static object Text(string caption = Caption, string body = Body, int id = 0) => new { id, caption, body };

        private static Task<HttpResponseMessage> Create(HttpClient client, object?[] sourceTexts, params object[] questions) =>
            client.PostAsJsonAsync("/api/quiz/create", new
            {
                quiz = new { title = "Kviz", isPrivate = false },
                sourceTexts,
                questions,
            });

        private static async Task<JsonElement> Created(HttpClient client, object[] sourceTexts, params object[] questions)
        {
            var response = await Create(client, sourceTexts, questions);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return await Get(client, await response.Content.ReadFromJsonAsync<int>());
        }

        private static Task<HttpResponseMessage> Update(HttpClient client, int quizId, object[]? sourceTexts, params object[] questions) =>
            client.PutAsJsonAsync("/api/quiz/update", new
            {
                quiz = new { id = quizId, title = "Kviz", isPrivate = false },
                sourceTexts,
                questions = questions.Length > 0 ? questions : null,
            });

        private static Task<JsonElement> Get(HttpClient client, int quizId) =>
            client.GetFromJsonAsync<JsonElement>($"/api/quiz/get/{quizId}");

        private static int Id(JsonElement element) => element.GetProperty("id").GetInt32();

        private static int[] QuestionIds(JsonElement quiz) =>
            quiz.GetProperty("questions").EnumerateArray().Select(Id).ToArray();

        private static JsonElement OnlyText(JsonElement quizOrReview) =>
            Assert.Single(quizOrReview.GetProperty("sourceTexts").EnumerateArray());

        // A full play of the quiz as it is now; the review of the stored game.
        private static async Task<JsonElement> Play(HttpClient student, int quizId)
        {
            var quiz = await Get(student, quizId);
            var response = await student.PostAsJsonAsync("/api/quiz/submit", new
            {
                quizId,
                answers = QuestionIds(quiz).Select(questionId => new { questionId, chosenOption = 1 }),
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return await Review(student, Id(await response.Content.ReadFromJsonAsync<JsonElement>()));
        }

        private static Task<JsonElement> Review(HttpClient student, int gameId) =>
            student.GetFromJsonAsync<JsonElement>($"/api/quiz/getGame/{gameId}");

        // null: the row is gone. Otherwise whether the stored source text is retired.
        private bool? IsRetired(int sourceTextId)
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();
            return context.SourceTexts.AsNoTracking().Where(s => s.Id == sourceTextId)
                .Select(s => (bool?)s.IsRetired).FirstOrDefault();
        }

        [Fact]
        public async Task A_quiz_is_saved_with_a_source_text_and_its_questions_point_to_it()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);

            var quiz = await Created(teacher, [Text()], Question("Prvo", 1), Question("Drugo", 1), Question("Treće"));

            var text = OnlyText(quiz);
            Assert.Equal(Caption, text.GetProperty("caption").GetString());
            Assert.Equal(Body, text.GetProperty("body").GetString());
            var questions = quiz.GetProperty("questions").EnumerateArray().ToArray();
            Assert.Equal(Id(text), questions[0].GetProperty("sourceTextId").GetInt32());
            Assert.Equal(Id(text), questions[1].GetProperty("sourceTextId").GetInt32());
            Assert.Equal(JsonValueKind.Null, questions[2].GetProperty("sourceTextId").ValueKind);
        }

        [Fact]
        public async Task A_quiz_without_source_texts_has_an_empty_list()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);

            var response = await teacher.PostAsJsonAsync("/api/quiz/create", new
            {
                quiz = new { title = "Kviz", isPrivate = false },
                questions = new[] { Question("Prvo") },
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var quiz = await Get(teacher, await response.Content.ReadFromJsonAsync<int>());
            Assert.Equal(0, quiz.GetProperty("sourceTexts").GetArrayLength());
        }

        [Theory]
        [InlineData(0, 10)]
        [InlineData(10, 0)]
        [InlineData(201, 10)]
        [InlineData(10, 8001)]
        public async Task A_source_text_needs_a_caption_and_a_body_of_a_bounded_length(int captionLength, int bodyLength)
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);

            var response = await Create(teacher, [Text(new string('a', captionLength), new string('b', bodyLength))], Question("Prvo", 1));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task A_caption_of_200_and_a_body_of_8000_characters_are_accepted()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);

            var response = await Create(teacher, [Text(new string('a', 200), new string('b', 8000))], Question("Prvo", 1));

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(11)]
        public async Task A_source_text_needs_between_one_and_ten_questions(int questionCount)
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var questions = Enumerable.Range(1, questionCount).Select(i => Question($"Pitanje {i}", 1)).Append(Question("Samostalno"));

            var response = await Create(teacher, [Text()], questions.ToArray());

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("between one and ten questions", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task A_source_texts_questions_must_be_together()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);

            var response = await Create(teacher, [Text()], Question("Prvo", 1), Question("Samostalno"), Question("Drugo", 1));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("must be together", await response.Content.ReadAsStringAsync());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        public async Task A_question_cannot_name_a_source_text_that_was_not_sent(int sourceTextNo)
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);

            var response = await Create(teacher, [Text()], Question("Prvo", 1), Question("Drugo", sourceTextNo));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task A_null_in_the_source_texts_list_is_a_bad_request()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);

            var response = await Create(teacher, [Text(), null], Question("Prvo", 1));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("must not contain null", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task A_body_keeps_its_line_breaks_and_loses_the_spaces_around_it()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);

            var quiz = await Created(teacher, [Text("  Naslov ", "  Prvi stih\r\n  Drugi stih\rTreći stih \n")], Question("Prvo", 1));

            var text = OnlyText(quiz);
            Assert.Equal("Naslov", text.GetProperty("caption").GetString());
            Assert.Equal("Prvi stih\n  Drugi stih\nTreći stih", text.GetProperty("body").GetString());
        }

        [Fact]
        public async Task A_played_source_text_that_is_edited_is_kept_for_the_review_of_that_play()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var quiz = await Created(teacher, [Text()], Question("Prvo", 1));
            var questionId = QuestionIds(quiz)[0];
            var firstTextId = Id(OnlyText(quiz));
            var firstPlay = await Play(student, Id(quiz));

            var edit = await Update(teacher, Id(quiz), [Text(body: "Drugi tekst", id: firstTextId)], Question("Prvo", 1, questionId));
            var secondTextId = Id(OnlyText(await Get(teacher, Id(quiz))));
            var secondPlay = await Play(student, Id(quiz));
            // Edited again: every play keeps the version it was played with.
            await Update(teacher, Id(quiz), [Text(body: "Treći tekst", id: secondTextId)], Question("Prvo", 1, questionId));

            Assert.Equal(HttpStatusCode.NoContent, edit.StatusCode);
            Assert.NotEqual(firstTextId, secondTextId);
            Assert.True(IsRetired(firstTextId));
            Assert.True(IsRetired(secondTextId));
            Assert.Equal("Treći tekst", OnlyText(await Get(teacher, Id(quiz))).GetProperty("body").GetString());
            var firstReview = await Review(student, Id(firstPlay));
            Assert.Equal(Body, OnlyText(firstReview).GetProperty("body").GetString());
            Assert.Equal(firstTextId, firstReview.GetProperty("answers")[0].GetProperty("sourceTextId").GetInt32());
            var secondReview = await Review(student, Id(secondPlay));
            Assert.Equal("Drugi tekst", OnlyText(secondReview).GetProperty("body").GetString());
            Assert.Equal(secondTextId, secondReview.GetProperty("answers")[0].GetProperty("sourceTextId").GetInt32());
        }

        [Fact]
        public async Task An_unplayed_source_text_is_overwritten_and_keeps_its_id()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var quiz = await Created(teacher, [Text()], Question("Prvo", 1));
            var textId = Id(OnlyText(quiz));

            var edit = await Update(teacher, Id(quiz), [Text("Novi naslov", "Novi tekst", textId)], Question("Prvo", 1, QuestionIds(quiz)[0]));

            Assert.Equal(HttpStatusCode.NoContent, edit.StatusCode);
            var text = OnlyText(await Get(teacher, Id(quiz)));
            Assert.Equal(textId, Id(text));
            Assert.Equal("Novi naslov", text.GetProperty("caption").GetString());
            Assert.Equal("Novi tekst", text.GetProperty("body").GetString());
        }

        [Fact]
        public async Task An_unchanged_played_source_text_stays_the_same_row()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var quiz = await Created(teacher, [Text()], Question("Prvo", 1));
            var textId = Id(OnlyText(quiz));
            await Play(student, Id(quiz));

            await Update(teacher, Id(quiz), [Text(id: textId)], Question("Prvo, drugim riječima", 1, QuestionIds(quiz)[0]));

            Assert.Equal(textId, Id(OnlyText(await Get(teacher, Id(quiz)))));
            Assert.False(IsRetired(textId));
        }

        [Fact]
        public async Task A_played_source_text_left_out_is_retired_and_an_unplayed_one_is_deleted()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var quiz = await Created(teacher, [Text()], Question("Prvo", 1));
            var playedId = Id(OnlyText(quiz));
            var play = await Play(student, Id(quiz));
            await Update(teacher, Id(quiz), [Text(id: playedId), Text("Drugi", "Drugi tekst")],
                Question("Prvo", 1, QuestionIds(quiz)[0]), Question("Drugo", 2));
            var withBoth = await Get(teacher, Id(quiz));
            var unplayedId = withBoth.GetProperty("sourceTexts").EnumerateArray().Select(Id).Single(id => id != playedId);

            // Questions sent without source texts: the quiz has none any more.
            var ids = QuestionIds(withBoth);
            var edit = await Update(teacher, Id(quiz), null, Question("Prvo", id: ids[0]), Question("Drugo", id: ids[1]));

            Assert.Equal(HttpStatusCode.NoContent, edit.StatusCode);
            var stored = await Get(teacher, Id(quiz));
            Assert.Equal(0, stored.GetProperty("sourceTexts").GetArrayLength());
            Assert.All(stored.GetProperty("questions").EnumerateArray(),
                question => Assert.Equal(JsonValueKind.Null, question.GetProperty("sourceTextId").ValueKind));
            Assert.True(IsRetired(playedId));
            Assert.Null(IsRetired(unplayedId));
            Assert.Equal(Body, OnlyText(await Review(student, Id(play))).GetProperty("body").GetString());
        }

        [Fact]
        public async Task A_source_text_of_another_quiz_is_refused()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var other = await Created(await factory.LoginAs(ApiFactory.OtherTeacherEmail), [Text()], Question("Tuđe", 1));
            var quiz = await Created(teacher, [Text()], Question("Prvo", 1));

            var response = await Update(teacher, Id(quiz), [Text(body: "Prepisano", id: Id(OnlyText(other)))], Question("Prvo", 1, QuestionIds(quiz)[0]));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal(Body, OnlyText(await Get(teacher, Id(other))).GetProperty("body").GetString());
            Assert.Equal(Id(OnlyText(quiz)), Id(OnlyText(await Get(teacher, Id(quiz)))));
        }

        [Fact]
        public async Task The_same_source_text_sent_twice_is_refused()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var quiz = await Created(teacher, [Text()], Question("Prvo", 1));
            var textId = Id(OnlyText(quiz));

            var response = await Update(teacher, Id(quiz), [Text(id: textId), Text(id: textId)],
                Question("Prvo", 1, QuestionIds(quiz)[0]), Question("Drugo", 2));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task An_update_without_questions_leaves_the_source_texts_alone()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var quiz = await Created(teacher, [Text()], Question("Prvo", 1));

            var response = await Update(teacher, Id(quiz), []);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            var stored = await Get(teacher, Id(quiz));
            Assert.Equal(Id(OnlyText(quiz)), Id(OnlyText(stored)));
            Assert.Equal(Id(OnlyText(quiz)), stored.GetProperty("questions")[0].GetProperty("sourceTextId").GetInt32());
        }

        [Fact]
        public async Task A_question_added_to_a_stored_source_text_points_to_it()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var quiz = await Created(teacher, [Text()], Question("Prvo", 1));
            var textId = Id(OnlyText(quiz));

            await Update(teacher, Id(quiz), [Text(id: textId)], Question("Prvo", 1, QuestionIds(quiz)[0]), Question("Drugo", 1));

            var stored = await Get(teacher, Id(quiz));
            Assert.All(stored.GetProperty("questions").EnumerateArray(),
                question => Assert.Equal(textId, question.GetProperty("sourceTextId").GetInt32()));
        }

        [Fact]
        public async Task Deleting_a_quiz_deletes_its_source_texts_and_plays()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var quiz = await Created(teacher, [Text()], Question("Prvo", 1));
            var firstTextId = Id(OnlyText(quiz));
            var play = await Play(student, Id(quiz));
            // A retired version and a current one.
            await Update(teacher, Id(quiz), [Text(body: "Drugi tekst", id: firstTextId)], Question("Prvo", 1, QuestionIds(quiz)[0]));
            var secondTextId = Id(OnlyText(await Get(teacher, Id(quiz))));

            var response = await teacher.DeleteAsync($"/api/quiz/delete/{Id(quiz)}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Null(IsRetired(firstTextId));
            Assert.Null(IsRetired(secondTextId));
            Assert.Equal(HttpStatusCode.NotFound, (await student.GetAsync($"/api/quiz/getGame/{Id(play)}")).StatusCode);
        }

        [Fact]
        public async Task The_question_report_names_each_questions_source_text()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var quiz = await Created(teacher, [Text()], Question("Prvo", 1), Question("Drugo"));

            var analytics = await teacher.GetFromJsonAsync<JsonElement>($"/api/quiz/getAnalytics/{Id(quiz)}");

            var rows = analytics.GetProperty("questions").EnumerateArray().ToArray();
            Assert.Equal(Caption, rows[0].GetProperty("sourceTextCaption").GetString());
            Assert.Equal(JsonValueKind.Null, rows[1].GetProperty("sourceTextCaption").ValueKind);
        }

        [Fact]
        public async Task A_private_quizs_source_text_comes_with_it_by_entry_code()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var response = await teacher.PostAsJsonAsync("/api/quiz/create", new
            {
                quiz = new { title = "Kviz", isPrivate = true },
                sourceTexts = new[] { Text() },
                questions = new[] { Question("Prvo", 1) },
            });
            var code = (await Get(teacher, await response.Content.ReadFromJsonAsync<int>())).GetProperty("entryCode").GetInt32();

            var byCode = await factory.CreateHttpsClient().GetFromJsonAsync<JsonElement>($"/api/quiz/getByCode/{code}");

            Assert.Equal(Body, OnlyText(byCode).GetProperty("body").GetString());
        }
    }
}
