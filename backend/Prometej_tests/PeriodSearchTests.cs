using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Prometej_tests
{
    // Each test writes one Period of its own, with a word no other test uses: the tests of a
    // class share one database.
    public class PeriodSearchTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        private static string Word() => $"w{Guid.NewGuid():N}"[..12];

        private async Task Write(int periodId, string content)
        {
            var admin = await factory.LoginAs(ApiFactory.AdminEmail);
            var response = await admin.PostAsJsonAsync("/api/period/content", new { id = 0, periodId, content });
            response.EnsureSuccessStatusCode();
        }

        private Task<HttpResponseMessage> Get(string query) =>
            factory.CreateHttpsClient().GetAsync($"/api/period/content/search?query={Uri.EscapeDataString(query)}");

        private async Task<List<JsonElement>> Search(string query)
        {
            var response = await Get(query);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().ToList();
        }

        private async Task<bool> Finds(string query, int periodId) =>
            (await Search(query)).Any(hit => hit.GetProperty("periodId").GetInt32() == periodId);

        private static (string? Heading, string Text)[] Passages(JsonElement hit) =>
            hit.GetProperty("passages").EnumerateArray()
                .Select(p => (p.GetProperty("heading").GetString(), p.GetProperty("text").GetString()!)).ToArray();

        [Fact]
        public async Task Search_ignores_case_and_diacritics()
        {
            var (accented, plain, stroke) = (Word(), Word(), Word());
            await Write(1, $"<p>Šenoa {accented}</p><p>Senoa {plain}</p><p>Đuro {stroke}</p>");

            Assert.True(await Finds($"šenoa {accented}", 1));
            Assert.True(await Finds($"senoa {accented}", 1));
            Assert.True(await Finds($"SENOA {accented.ToUpperInvariant()}", 1));
            Assert.True(await Finds($"šenoa {plain}", 1));
            Assert.True(await Finds($"duro {stroke}", 1));
            // "đ" is read as "d", not as the "dj" some people type for it.
            Assert.False(await Finds($"djuro {stroke}", 1));
        }

        [Fact]
        public async Task Search_matches_the_text_and_never_the_markup()
        {
            var word = Word();
            await Write(2, $"<p class=\"note\">Au<strong>gust</strong> {word}&nbsp;piše: Držić &amp; Marulić</p>");

            foreach (var markup in new[] { "strong", "class", "note", "nbsp", "amp" })
            {
                Assert.False(await Finds(markup, 2), markup);
            }

            Assert.True(await Finds("držić & marulić", 2));
            // A word split by inline markup is one word, and the passage is plain text.
            var hit = Assert.Single(await Search($"august {word}"));
            Assert.Equal([(null, $"August {word} piše: Držić & Marulić")], Passages(hit));
        }

        [Fact]
        public async Task A_passage_carries_the_nearest_heading_above_it()
        {
            var word = Word();
            await Write(3, $"<p>{word} prije naslova</p><h2>Prvi naslov</h2><p>Nešto drugo</p><p>{word} ispod naslova</p><h3>Podnaslov {word}</h3>");

            var hit = Assert.Single(await Search(word));

            Assert.Equal(3, hit.GetProperty("periodId").GetInt32());
            Assert.Equal(3, hit.GetProperty("matchCount").GetInt32());
            Assert.Equal(
                [(null, $"{word} prije naslova"), ("Prvi naslov", $"{word} ispod naslova"), (null, $"Podnaslov {word}")],
                Passages(hit));
        }

        [Fact]
        public async Task A_period_returns_its_first_three_passages_and_the_number_of_matches()
        {
            var word = Word();
            await Write(4, $"<h2>Popis</h2><ul><li>{word} jedan</li><li><p>{word} dva</p></li></ul>"
                + $"<h3>Dalje</h3><p>{word} tri</p><p>{word} četiri</p><p>{word} pet</p>");

            var hit = Assert.Single(await Search(word));

            Assert.Equal(5, hit.GetProperty("matchCount").GetInt32());
            Assert.Equal(
                [("Popis", $"{word} jedan"), ("Popis", $"{word} dva"), ("Dalje", $"{word} tri")],
                Passages(hit));
        }

        [Fact]
        public async Task A_long_paragraph_is_cut_to_the_words_around_the_match()
        {
            var word = Word();
            var filler = string.Concat(Enumerable.Repeat("riječ ", 170));
            await Write(5, $"<p>{filler}{word} {filler}</p>");

            var hit = Assert.Single(await Search(word));

            var (_, text) = Assert.Single(Passages(hit));
            Assert.InRange(text.Length, 200, 302);
            Assert.Contains($" {word} ", text);
            Assert.StartsWith("…riječ ", text);
            Assert.EndsWith(" riječ…", text);
        }

        [Fact]
        public async Task A_query_that_cannot_match_returns_nothing_and_one_too_long_is_refused()
        {
            await Write(6, "<p>Antika</p>");

            Assert.Empty(await Search(Word()));
            Assert.Empty(await Search("a"));
            Assert.Empty(await Search(" a "));
            var noQuery = await factory.CreateHttpsClient().GetAsync("/api/period/content/search");
            var tooLong = await Get(new string('a', 101));

            Assert.Equal(HttpStatusCode.OK, noQuery.StatusCode);
            Assert.Equal("[]", await noQuery.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.BadRequest, tooLong.StatusCode);
        }

        [Fact]
        public async Task Search_survives_a_query_with_an_apostrophe_or_a_url_character()
        {
            await Write(7, "<h2>Romantizam</h2><p>Piše O'Brien o romantizmu.</p>");

            var hit = Assert.Single(await Search("O'Brien"));

            Assert.Equal(7, hit.GetProperty("periodId").GetInt32());
            Assert.Equal([("Romantizam", "Piše O'Brien o romantizmu.")], Passages(hit));
            foreach (var query in new[] { "a/b", "100%", "a#b", "tko?", "a\\b" })
            {
                Assert.Equal(HttpStatusCode.OK, (await Get(query)).StatusCode);
            }
        }
    }
}
