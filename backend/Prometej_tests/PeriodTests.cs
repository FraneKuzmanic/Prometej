using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Prometej_tests
{
    public class PeriodTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        [Fact]
        public async Task Anyone_can_list_the_periods_in_curriculum_order()
        {
            var periods = (await factory.CreateHttpsClient().GetFromJsonAsync<JsonElement>("/api/period"))
                .EnumerateArray().ToList();

            // Romantizam (5) comes before Realizam (4): an id is not a place in the order.
            Assert.Equal(
                [
                    "Antika", "Srednji vijek", "Humanizam i predrenesansa", "Renesansa", "Barok", "Klasicizam",
                    "Predromantizam", "Romantizam", "Realizam", "Modernizam", "Ekspresionizam", "Suvremena književnost",
                ],
                periods.Select(p => p.GetProperty("name").GetString()));
            Assert.Equal([1, 2, 7, 3, 8, 9, 10, 5, 4, 6, 11, 12], periods.Select(p => p.GetProperty("id").GetInt32()));

            var antika = periods[0];
            Assert.Equal("antika.webp", antika.GetProperty("image").GetString());
            Assert.NotEqual("", antika.GetProperty("timeFrame").GetString());
            Assert.NotEqual("", antika.GetProperty("description").GetString());
            var barok = periods.Single(p => p.GetProperty("name").GetString() == "Barok");
            Assert.Equal("barok.webp", barok.GetProperty("image").GetString());
            Assert.All(periods, p => Assert.EndsWith(".webp", p.GetProperty("image").GetString()));
        }

        [Fact]
        public async Task Content_cannot_be_written_for_a_period_that_does_not_exist()
        {
            var admin = await factory.LoginAs(ApiFactory.AdminEmail);

            var unknown = await admin.PostAsJsonAsync("/api/period/content", new { id = 0, periodId = 99, content = "<p>Nema ga</p>" });
            var zero = await admin.PostAsJsonAsync("/api/period/content", new { id = 0, periodId = 0, content = "<p>Nema ga</p>" });
            var read = await admin.GetAsync("/api/period/content/99");

            Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, zero.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
        }

        [Fact]
        public async Task A_period_is_listed_with_the_number_of_its_topics()
        {
            var client = factory.CreateHttpsClient();
            async Task<Dictionary<int, int>> TopicCounts() =>
                (await client.GetFromJsonAsync<JsonElement>("/api/period")).EnumerateArray()
                    .ToDictionary(p => p.GetProperty("id").GetInt32(), p => p.GetProperty("topicCount").GetInt32());

            var before = await TopicCounts();
            factory.AddTopics(periodId: 9, count: 2);
            var after = await TopicCounts();

            Assert.All(before.Values, count => Assert.Equal(0, count));
            Assert.Equal(2, after[9]);
            Assert.Equal(0, after[8]);
        }
    }
}
