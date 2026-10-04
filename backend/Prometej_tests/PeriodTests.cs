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
            Assert.Equal("antika.png", antika.GetProperty("image").GetString());
            Assert.NotEqual("", antika.GetProperty("timeFrame").GetString());
            Assert.NotEqual("", antika.GetProperty("description").GetString());
            var barok = periods.Single(p => p.GetProperty("name").GetString() == "Barok");
            Assert.Equal(JsonValueKind.Null, barok.GetProperty("image").ValueKind);
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
    }
}
