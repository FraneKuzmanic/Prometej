using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Prometej_tests
{
    // Each test uses its own Period ids: the tests of a class share one database.
    public class PeriodAuthorizationTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        private static object Content(int periodId, string content, int id = 0) => new { id, periodId, content };

        private static async Task<JsonElement> GetContent(HttpClient client, int periodId) =>
            await client.GetFromJsonAsync<JsonElement>($"/api/period/content/{periodId}");

        [Fact]
        public async Task Only_an_admin_can_write_period_content()
        {
            var body = Content(3, "<p>Renesansa</p>");

            var anonymous = await factory.CreateHttpsClient().PostAsJsonAsync("/api/period/content", body);
            var student = await (await factory.LoginAsNewStudent()).PostAsJsonAsync("/api/period/content", body);
            var teacher = await (await factory.LoginAs(ApiFactory.TeacherEmail)).PostAsJsonAsync("/api/period/content", body);
            var admin = await (await factory.LoginAs(ApiFactory.AdminEmail)).PostAsJsonAsync("/api/period/content", body);

            Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, student.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, teacher.StatusCode);
            Assert.Equal(HttpStatusCode.OK, admin.StatusCode);
        }

        [Fact]
        public async Task Anyone_can_read_period_content_and_an_empty_period_is_not_found()
        {
            var admin = await factory.LoginAs(ApiFactory.AdminEmail);
            await admin.PostAsJsonAsync("/api/period/content", Content(4, "<p>Realizam</p>"));
            var anonymous = factory.CreateHttpsClient();

            var written = await GetContent(anonymous, 4);
            var empty = await anonymous.GetAsync("/api/period/content/6");

            Assert.Equal("<p>Realizam</p>", written.GetProperty("content").GetString());
            Assert.Equal(HttpStatusCode.NotFound, empty.StatusCode);
        }

        [Fact]
        public async Task A_stale_row_id_cannot_overwrite_another_periods_content()
        {
            var admin = await factory.LoginAs(ApiFactory.AdminEmail);
            await admin.PostAsJsonAsync("/api/period/content", Content(1, "<p>Antika</p>"));
            var antikaRowId = (await GetContent(admin, 1)).GetProperty("id").GetInt32();

            // What the client used to send after opening Period 1 and then an empty Period 2.
            var response = await admin.PostAsJsonAsync("/api/period/content", Content(2, "<p>Srednji vijek</p>", antikaRowId));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("<p>Antika</p>", (await GetContent(admin, 1)).GetProperty("content").GetString());
            Assert.Equal("<p>Srednji vijek</p>", (await GetContent(admin, 2)).GetProperty("content").GetString());
        }

        [Fact]
        public async Task Saving_a_period_twice_updates_its_one_content()
        {
            var admin = await factory.LoginAs(ApiFactory.AdminEmail);

            var first = await admin.PostAsJsonAsync("/api/period/content", Content(7, "<p>Prva verzija</p>"));
            var second = await admin.PostAsJsonAsync("/api/period/content", Content(7, "<p>Druga verzija</p>"));

            Assert.Equal(await first.Content.ReadFromJsonAsync<int>(), await second.Content.ReadFromJsonAsync<int>());
            Assert.Equal("<p>Druga verzija</p>", (await GetContent(admin, 7)).GetProperty("content").GetString());
        }
    }
}
