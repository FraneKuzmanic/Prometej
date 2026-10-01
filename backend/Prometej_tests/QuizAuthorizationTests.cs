using System.Net;

namespace Prometej_tests
{
    public class QuizAuthorizationTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        [Fact]
        public async Task Public_quiz_list_is_open_to_anonymous_callers()
        {
            var client = factory.CreateHttpsClient();

            var response = await client.GetAsync("/api/quiz/getAll");

            Assert.True(response.IsSuccessStatusCode);
            // A fresh container has no quizzes: proves the tests do not run against the local database.
            Assert.Equal("[]", await response.Content.ReadAsStringAsync());
        }
    }
}
