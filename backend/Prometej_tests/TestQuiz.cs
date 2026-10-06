using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Prometej_tests
{
    // What the two classes about tests and sittings both need: a quiz of each kind of
    // question, and the calls of a sitting.
    internal static class TestQuiz
    {
        public record Created(int Id, int Code, int[] QuestionIds);

        // Four works and their authors; the right-hand options are numbered 1 to 4 in this
        // order, and the one extra is number 5.
        public static readonly (string Left, string Right)[] Pairs =
        [
            ("Judita", "Marko Marulić"),
            ("Planine", "Petar Zoranić"),
            ("Dundo Maroje", "Marin Držić"),
            ("Osman", "Ivan Gundulić"),
        ];

        public const string Extra = "Hanibal Lucić";

        // Five works in the order they were written.
        public static readonly string[] Items = ["Judita", "Planine", "Dundo Maroje", "Osman", "Smrt Smail-age Čengića"];

        // The options are A to D and the first one is correct.
        public static object Choice(string title = "Izbor", int id = 0) => new
        {
            id,
            questionTitle = title,
            firstAnswer = "A",
            secondAnswer = "B",
            thirdAnswer = "C",
            fourthAnswer = "D",
            correctOption = 1,
            hintText = "Pomoć",
            exploreMore = "Objašnjenje",
        };

        public static object Matching(int id = 0) => new
        {
            id,
            type = "matching",
            questionTitle = "Poveži djelo s autorom.",
            content = new { pairs = Pairs.Select(pair => new { left = pair.Left, right = pair.Right }), extras = new[] { Extra } },
        };

        public static object Ordering(int id = 0) => new
        {
            id,
            type = "ordering",
            questionTitle = "Poredaj djela po vremenu nastanka.",
            content = new { items = Items },
        };

        // A choice, a matching and an ordering question: ten points.
        public static object[] OneOfEach => [Choice(), Matching(), Ordering()];

        public static object Header(int id = 0, bool isPrivate = true, bool isTest = true, int? timeLimitMinutes = null, DateTimeOffset? closesAt = null, string title = "Provjera") =>
            new { id, title, isPrivate, isTest, timeLimitMinutes, closesAt };

        public static Task<HttpResponseMessage> Create(HttpClient client, object quiz, params object[] questions) =>
            client.PostAsJsonAsync("/api/quiz/create", new { quiz, questions });

        public static Task<HttpResponseMessage> UpdateHeader(HttpClient client, object quiz) =>
            client.PutAsJsonAsync("/api/quiz/update", new { quiz });

        public static async Task<Created> CreateTest(HttpClient teacher, object[]? questions = null, int? timeLimitMinutes = null, DateTimeOffset? closesAt = null) =>
            await Stored(teacher, await Create(teacher, Header(timeLimitMinutes: timeLimitMinutes, closesAt: closesAt), questions ?? OneOfEach));

        // The quiz as its creator reads it: with its entry code and its questions.
        public static async Task<Created> Stored(HttpClient creator, HttpResponseMessage created)
        {
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            var quiz = await Get(creator, await created.Content.ReadFromJsonAsync<int>());
            var code = quiz.GetProperty("entryCode");

            return new Created(
                quiz.GetProperty("id").GetInt32(),
                code.ValueKind == JsonValueKind.Null ? 0 : code.GetInt32(),
                quiz.GetProperty("questions").EnumerateArray().Select(q => q.GetProperty("id").GetInt32()).ToArray());
        }

        public static Task<JsonElement> Get(HttpClient client, int quizId) =>
            client.GetFromJsonAsync<JsonElement>($"/api/quiz/get/{quizId}");

        public static Task<HttpResponseMessage> Info(HttpClient client, int quizId, int? code) =>
            client.GetAsync($"/api/sitting/info/{quizId}?code={code}");

        public static async Task<JsonElement> InfoOf(HttpClient client, Created quiz)
        {
            var response = await Info(client, quiz.Id, quiz.Code);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }

        public static Task<HttpResponseMessage> Start(HttpClient client, int quizId, int? code) =>
            client.PostAsync($"/api/sitting/start/{quizId}?code={code}", null);

        public static async Task<JsonElement> Started(HttpClient client, Created quiz)
        {
            var response = await Start(client, quiz.Id, quiz.Code);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }

        public static Task<HttpResponseMessage> Save(HttpClient client, int sittingId, int questionId, params int[] given) =>
            client.PutAsJsonAsync($"/api/sitting/{sittingId}/answer", new { questionId, given });

        public static async Task Saved(HttpClient client, int sittingId, int questionId, params int[] given) =>
            Assert.Equal(HttpStatusCode.NoContent, (await Save(client, sittingId, questionId, given)).StatusCode);

        public static Task<HttpResponseMessage> Finish(HttpClient client, int sittingId) =>
            client.PostAsync($"/api/sitting/{sittingId}/finish", null);

        public static async Task<JsonElement> Finished(HttpClient client, int sittingId)
        {
            var response = await Finish(client, sittingId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }

        public static Task<JsonElement> Analytics(HttpClient client, int quizId) =>
            client.GetFromJsonAsync<JsonElement>($"/api/quiz/getAnalytics/{quizId}");

        public static async Task<JsonElement[]> Games(HttpClient client, int quizId) =>
            (await Analytics(client, quizId)).GetProperty("games").EnumerateArray().ToArray();

        public static int Id(JsonElement element) => element.GetProperty("id").GetInt32();

        public static string[] Texts(JsonElement element, string property) =>
            element.GetProperty(property).EnumerateArray().Select(text => text.GetString()!).ToArray();

        // The question of a sitting with this id.
        public static JsonElement Question(JsonElement sitting, int questionId) =>
            sitting.GetProperty("questions").EnumerateArray().Single(q => q.GetProperty("questionId").GetInt32() == questionId);

        // For each wanted text, the number (from 1) it has in the list the sitting showed.
        public static int[] ShownNumbers(JsonElement question, string list, IEnumerable<string> wanted)
        {
            var shown = Texts(question, list);

            return wanted.Select(text => Array.IndexOf(shown, text) + 1).ToArray();
        }
    }
}
