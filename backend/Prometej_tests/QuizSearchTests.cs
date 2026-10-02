using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Prometej_tests
{
    // Each test's titles carry a word no other test uses: the tests of a class share one database.
    public class QuizSearchTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        private static string Word() => $"w{Guid.NewGuid():N}"[..12];

        private static async Task CreateQuiz(HttpClient client, string title, bool isPrivate = false)
        {
            var question = new
            {
                questionTitle = "Pitanje",
                firstAnswer = "A",
                secondAnswer = "B",
                thirdAnswer = "C",
                fourthAnswer = "D",
                correctOption = 1,
            };
            var response = await client.PostAsJsonAsync("/api/quiz/create",
                new { quiz = new { title, isPrivate }, questions = new[] { question } });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        private Task<HttpResponseMessage> Get(string query) =>
            factory.CreateHttpsClient().GetAsync($"/api/quiz/search?query={Uri.EscapeDataString(query)}");

        private async Task<string[]> Search(string query)
        {
            var response = await Get(query);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray()
                .Select(quiz => quiz.GetProperty("title").GetString()!).ToArray();
        }

        [Fact]
        public async Task Search_by_title_ignores_case_and_diacritics()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var (accented, plain, stroke) = (Word(), Word(), Word());
            await CreateQuiz(teacher, $"Šenoa {accented}");
            await CreateQuiz(teacher, $"Senoa {plain}");
            await CreateQuiz(teacher, $"Đuro {stroke}");

            Assert.Equal([$"Šenoa {accented}"], await Search($"šenoa {accented}"));
            Assert.Equal([$"Šenoa {accented}"], await Search($"senoa {accented}"));
            Assert.Equal([$"Šenoa {accented}"], await Search($"  SENOA {accented.ToUpperInvariant()} "));
            Assert.Equal([$"Senoa {plain}"], await Search($"šenoa {plain}"));
            Assert.Equal([$"Đuro {stroke}"], await Search($"duro {stroke}"));
            // "đ" is read as "d", not as the "dj" some people type for it.
            Assert.Empty(await Search($"djuro {stroke}"));
        }

        [Fact]
        public async Task Search_finds_a_quiz_by_its_creators_name()
        {
            var word = Word();
            await CreateQuiz(await factory.LoginAs(ApiFactory.TeacherEmail), $"Prvi {word}");
            await CreateQuiz(await factory.LoginAs(ApiFactory.OtherTeacherEmail), $"Drugi {word}");

            foreach (var name in new[] { "tea", "TEACHER", "tea teacher" })
            {
                var titles = await Search(name);

                Assert.Contains($"Prvi {word}", titles);
                Assert.DoesNotContain($"Drugi {word}", titles);
            }
        }

        [Fact]
        public async Task Search_never_returns_a_private_quiz()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var word = Word();
            await CreateQuiz(teacher, $"Privatni {word}", isPrivate: true);

            Assert.Empty(await Search(word));
            Assert.DoesNotContain($"Privatni {word}", await Search("tea teacher"));
        }

        [Fact]
        public async Task A_percent_sign_an_underscore_and_a_backslash_are_read_as_text()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var word = Word();
            await CreateQuiz(teacher, $"100% {word}");
            await CreateQuiz(teacher, $"a_b\\c {word}");
            await CreateQuiz(teacher, $"Obični {word}");

            Assert.Equal([$"100% {word}"], await Search($"100% {word}"));
            Assert.Equal([$"a_b\\c {word}"], await Search($"a_b\\c {word}"));
            Assert.DoesNotContain($"Obični {word}", await Search("%"));
            Assert.DoesNotContain($"Obični {word}", await Search("_"));
            Assert.Empty(await Search($"O_ični {word}"));
        }

        [Fact]
        public async Task A_blank_query_returns_every_public_quiz_and_one_too_long_is_refused()
        {
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var word = Word();
            await CreateQuiz(teacher, $"Javni {word}");
            await CreateQuiz(teacher, $"Privatni {word}", isPrivate: true);
            var anonymous = factory.CreateHttpsClient();

            var all = await anonymous.GetStringAsync("/api/quiz/getAll");
            var blank = await anonymous.GetStringAsync("/api/quiz/search?query=%20");
            var noQuery = await anonymous.GetStringAsync("/api/quiz/search");
            var tooLong = await Get(new string('a', 101));

            Assert.Contains($"Javni {word}", all);
            Assert.Equal(all, blank);
            Assert.Equal(all, noQuery);
            Assert.Equal(HttpStatusCode.BadRequest, tooLong.StatusCode);
            Assert.Empty(await Search(Word()));
        }
    }
}
