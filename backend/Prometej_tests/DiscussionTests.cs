using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Prometej_tests
{
    // One account may post five times a minute, a refused post included, so the students who
    // write are registered by each test. The seeded teacher writes once in this class and the
    // seeded admin never. The tests that read a whole list have a period of their own (7, 8).
    public class DiscussionTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        private const int Period = 1;
        private const int PeriodOfTheOrderTest = 7;
        private const int PeriodOfThePageTest = 8;
        private const int NoSuchId = 999999;

        private static Task<HttpResponseMessage> PostTopic(HttpClient client, int periodId, string title = "Naslov", string body = "Tekst") =>
            client.PostAsJsonAsync($"/api/discussion/period/{periodId}", new { title, body });

        private static async Task<int> CreateTopic(HttpClient client, int periodId = Period, string title = "Naslov", string body = "Tekst")
        {
            var response = await PostTopic(client, periodId, title, body);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return await response.Content.ReadFromJsonAsync<int>();
        }

        private static Task<HttpResponseMessage> PostReply(HttpClient client, int topicId, string body = "Odgovor") =>
            client.PostAsJsonAsync($"/api/discussion/topic/{topicId}/reply", new { body });

        private static async Task<int> CreateReply(HttpClient client, int topicId, string body = "Odgovor")
        {
            var response = await PostReply(client, topicId, body);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return await response.Content.ReadFromJsonAsync<int>();
        }

        private static Task<JsonElement> Topics(HttpClient client, int periodId, int page = 1) =>
            client.GetFromJsonAsync<JsonElement>($"/api/discussion/period/{periodId}?page={page}");

        private static Task<JsonElement> Topic(HttpClient client, int id) =>
            client.GetFromJsonAsync<JsonElement>($"/api/discussion/topic/{id}");

        private static JsonElement[] Replies(JsonElement topic) =>
            topic.GetProperty("replies").EnumerateArray().ToArray();

        private static int[] Ids(JsonElement page) =>
            page.GetProperty("topics").EnumerateArray().Select(t => t.GetProperty("id").GetInt32()).ToArray();

        private static async Task<int> IdOf(HttpClient client) =>
            (await client.GetFromJsonAsync<JsonElement>("/api/user/me")).GetProperty("id").GetInt32();

        [Fact]
        public async Task Anyone_reads_a_periods_topics_and_a_topic()
        {
            var student = await factory.LoginAsNewStudent();
            var topicId = await CreateTopic(student, title: "Ideja Prijana Lovre");
            var anonymous = factory.CreateHttpsClient();

            var list = await anonymous.GetAsync($"/api/discussion/period/{Period}");
            var topic = await anonymous.GetAsync($"/api/discussion/topic/{topicId}");

            Assert.Equal(HttpStatusCode.OK, list.StatusCode);
            Assert.Equal(HttpStatusCode.OK, topic.StatusCode);
            Assert.Equal("Ideja Prijana Lovre", (await topic.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("title").GetString());
        }

        [Fact]
        public async Task An_unknown_period_topic_or_reply_is_not_found()
        {
            var anonymous = factory.CreateHttpsClient();
            var student = await factory.LoginAsNewStudent();

            Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync($"/api/discussion/period/{NoSuchId}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync($"/api/discussion/topic/{NoSuchId}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await PostTopic(student, NoSuchId)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await PostReply(student, NoSuchId)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await student.DeleteAsync($"/api/discussion/topic/{NoSuchId}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await student.DeleteAsync($"/api/discussion/reply/{NoSuchId}")).StatusCode);
        }

        [Fact]
        public async Task Writing_and_deleting_need_a_session()
        {
            var student = await factory.LoginAsNewStudent();
            var topicId = await CreateTopic(student);
            var replyId = await CreateReply(student, topicId);
            var anonymous = factory.CreateHttpsClient();

            Assert.Equal(HttpStatusCode.Unauthorized, (await PostTopic(anonymous, Period)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await PostReply(anonymous, topicId)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.DeleteAsync($"/api/discussion/topic/{topicId}")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.DeleteAsync($"/api/discussion/reply/{replyId}")).StatusCode);
        }

        [Fact]
        public async Task A_topic_is_stored_trimmed_with_its_authors_name_and_role()
        {
            var student = await factory.LoginAsNewStudent();

            var topicId = await CreateTopic(student, title: "  Tko je Lovro?  ", body: "  Prvi red.\r\nDrugi red.  ");
            var replyId = await CreateReply(student, topicId, "  Jedan\rdva  ");
            var topic = await Topic(student, topicId);

            Assert.Equal("Tko je Lovro?", topic.GetProperty("title").GetString());
            Assert.Equal("Prvi red.\nDrugi red.", topic.GetProperty("body").GetString());
            Assert.Equal(Period, topic.GetProperty("periodId").GetInt32());
            Assert.Equal("Sara Student", topic.GetProperty("authorName").GetString());
            Assert.Equal("student", topic.GetProperty("authorRole").GetString());
            var reply = Assert.Single(Replies(topic));
            Assert.Equal(replyId, reply.GetProperty("id").GetInt32());
            Assert.Equal("Jedan\ndva", reply.GetProperty("body").GetString());
            // Who may delete is the server's answer; no account id leaves it.
            Assert.False(topic.TryGetProperty("authorId", out _));
            Assert.False(reply.TryGetProperty("authorId", out _));
        }

        [Fact]
        public async Task A_teachers_post_carries_the_teachers_role()
        {
            var student = await factory.LoginAsNewStudent();
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var topicId = await CreateTopic(student);

            await CreateReply(teacher, topicId);

            var reply = Assert.Single(Replies(await Topic(student, topicId)));
            Assert.Equal("Tea Teacher", reply.GetProperty("authorName").GetString());
            Assert.Equal("teacher", reply.GetProperty("authorRole").GetString());
        }

        [Theory]
        [InlineData(0, 10, HttpStatusCode.BadRequest)]
        [InlineData(151, 10, HttpStatusCode.BadRequest)]
        [InlineData(150, 0, HttpStatusCode.BadRequest)]
        [InlineData(150, 2001, HttpStatusCode.BadRequest)]
        [InlineData(150, 2000, HttpStatusCode.Created)]
        public async Task A_title_or_a_body_outside_the_limits_is_refused(int titleLength, int bodyLength, HttpStatusCode expected)
        {
            var student = await factory.LoginAsNewStudent();

            var response = await PostTopic(student, Period, new string('a', titleLength), new string('b', bodyLength));

            Assert.Equal(expected, response.StatusCode);
        }

        [Fact]
        public async Task A_text_of_spaces_only_is_refused()
        {
            var student = await factory.LoginAsNewStudent();
            var topicId = await CreateTopic(student);

            Assert.Equal(HttpStatusCode.BadRequest, (await PostTopic(student, Period, "   ", "Tekst")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await PostTopic(student, Period, "Naslov", " \n ")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await PostReply(student, topicId, "   ")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await PostReply(student, topicId, new string('c', 2001))).StatusCode);
        }

        [Fact]
        public async Task Topics_are_listed_by_last_activity_with_their_reply_count()
        {
            var student = await factory.LoginAsNewStudent();
            var first = await CreateTopic(student, PeriodOfTheOrderTest, "Prva");
            var second = await CreateTopic(student, PeriodOfTheOrderTest, "Druga");

            var before = await Topics(student, PeriodOfTheOrderTest);
            await CreateReply(student, first);
            var after = await Topics(student, PeriodOfTheOrderTest);

            Assert.Equal([second, first], Ids(before));
            Assert.Equal([first, second], Ids(after));
            var replied = after.GetProperty("topics")[0];
            Assert.Equal("Prva", replied.GetProperty("title").GetString());
            Assert.Equal("Sara Student", replied.GetProperty("authorName").GetString());
            Assert.Equal(1, replied.GetProperty("replyCount").GetInt32());
            Assert.True(replied.GetProperty("lastActivityAt").GetDateTime() > replied.GetProperty("createdAt").GetDateTime());
            var quiet = after.GetProperty("topics")[1];
            Assert.Equal(0, quiet.GetProperty("replyCount").GetInt32());
            Assert.Equal(quiet.GetProperty("createdAt").GetDateTime(), quiet.GetProperty("lastActivityAt").GetDateTime());
        }

        [Fact]
        public async Task Topics_are_paged_by_twenty()
        {
            factory.AddTopics(PeriodOfThePageTest, 21);
            var anonymous = factory.CreateHttpsClient();

            var first = await Topics(anonymous, PeriodOfThePageTest);
            var second = await Topics(anonymous, PeriodOfThePageTest, 2);
            var third = await Topics(anonymous, PeriodOfThePageTest, 3);

            Assert.Equal(20, first.GetProperty("topics").GetArrayLength());
            Assert.Equal(21, first.GetProperty("total").GetInt32());
            Assert.Equal(20, first.GetProperty("pageSize").GetInt32());
            // The newest is first, so the oldest is alone on the second page.
            Assert.Equal("Tema 21", first.GetProperty("topics")[0].GetProperty("title").GetString());
            Assert.Equal("Tema 1", Assert.Single(second.GetProperty("topics").EnumerateArray()).GetProperty("title").GetString());
            Assert.Equal(0, third.GetProperty("topics").GetArrayLength());
            Assert.Equal(21, third.GetProperty("total").GetInt32());
            Assert.Equal(HttpStatusCode.BadRequest, (await anonymous.GetAsync($"/api/discussion/period/{PeriodOfThePageTest}?page=0")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await anonymous.GetAsync($"/api/discussion/period/{PeriodOfThePageTest}?page=abc")).StatusCode);
        }

        [Fact]
        public async Task Replies_are_listed_oldest_first()
        {
            var student = await factory.LoginAsNewStudent();
            var topicId = await CreateTopic(student);

            var first = await CreateReply(student, topicId, "Prvi");
            var second = await CreateReply(student, topicId, "Drugi");
            var third = await CreateReply(student, topicId, "Treći");

            var ids = Replies(await Topic(student, topicId)).Select(r => r.GetProperty("id").GetInt32());
            Assert.Equal([first, second, third], ids);
        }

        [Fact]
        public async Task An_author_deletes_their_reply_and_nobody_else_but_an_admin_does()
        {
            var author = await factory.LoginAsNewStudent();
            var other = await factory.LoginAsNewStudent();
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var admin = await factory.LoginAs(ApiFactory.AdminEmail);
            var topicId = await CreateTopic(other);
            var first = await CreateReply(author, topicId);
            var second = await CreateReply(author, topicId);

            Assert.Equal(HttpStatusCode.Forbidden, (await other.DeleteAsync($"/api/discussion/reply/{first}")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await teacher.DeleteAsync($"/api/discussion/reply/{first}")).StatusCode);
            Assert.Equal(2, Replies(await Topic(author, topicId)).Length);

            Assert.Equal(HttpStatusCode.NoContent, (await author.DeleteAsync($"/api/discussion/reply/{first}")).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/discussion/reply/{second}")).StatusCode);
            Assert.Empty(Replies(await Topic(author, topicId)));
        }

        [Fact]
        public async Task An_author_deletes_their_topic_only_while_it_has_no_replies()
        {
            var author = await factory.LoginAsNewStudent();
            var topicId = await CreateTopic(author);
            // Their own reply counts too: the rule is one sentence.
            var replyId = await CreateReply(author, topicId);

            var refused = await author.DeleteAsync($"/api/discussion/topic/{topicId}");
            await author.DeleteAsync($"/api/discussion/reply/{replyId}");
            var allowed = await author.DeleteAsync($"/api/discussion/topic/{topicId}");

            Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, allowed.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await author.GetAsync($"/api/discussion/topic/{topicId}")).StatusCode);
        }

        [Fact]
        public async Task Someone_elses_topic_cannot_be_deleted()
        {
            var author = await factory.LoginAsNewStudent();
            var other = await factory.LoginAsNewStudent();
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var topicId = await CreateTopic(author);

            Assert.Equal(HttpStatusCode.Forbidden, (await other.DeleteAsync($"/api/discussion/topic/{topicId}")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await teacher.DeleteAsync($"/api/discussion/topic/{topicId}")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await author.GetAsync($"/api/discussion/topic/{topicId}")).StatusCode);
        }

        [Fact]
        public async Task An_admin_deletes_a_topic_with_its_replies()
        {
            var author = await factory.LoginAsNewStudent();
            var other = await factory.LoginAsNewStudent();
            var admin = await factory.LoginAs(ApiFactory.AdminEmail);
            var topicId = await CreateTopic(author);
            var replyId = await CreateReply(other, topicId);

            var response = await admin.DeleteAsync($"/api/discussion/topic/{topicId}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/discussion/topic/{topicId}")).StatusCode);
            Assert.DoesNotContain(topicId, Ids(await Topics(admin, Period)));
            // The reply went with it.
            Assert.Equal(HttpStatusCode.NotFound, (await other.DeleteAsync($"/api/discussion/reply/{replyId}")).StatusCode);
        }

        [Fact]
        public async Task A_read_says_whether_the_caller_may_delete()
        {
            var author = await factory.LoginAsNewStudent();
            var other = await factory.LoginAsNewStudent();
            var admin = await factory.LoginAs(ApiFactory.AdminEmail);
            var anonymous = factory.CreateHttpsClient();
            var topicId = await CreateTopic(author);

            static bool CanDelete(JsonElement post) => post.GetProperty("canDelete").GetBoolean();

            Assert.True(CanDelete(await Topic(author, topicId)));
            Assert.False(CanDelete(await Topic(other, topicId)));
            Assert.False(CanDelete(await Topic(anonymous, topicId)));
            Assert.True(CanDelete(await Topic(admin, topicId)));

            await CreateReply(other, topicId);

            // With a reply under it, the topic is no longer its author's to delete.
            Assert.False(CanDelete(await Topic(author, topicId)));
            Assert.True(CanDelete(await Topic(admin, topicId)));
            Assert.False(CanDelete(Replies(await Topic(author, topicId))[0]));
            Assert.True(CanDelete(Replies(await Topic(other, topicId))[0]));
            Assert.False(CanDelete(Replies(await Topic(anonymous, topicId))[0]));
            Assert.True(CanDelete(Replies(await Topic(admin, topicId))[0]));
        }

        [Fact]
        public async Task A_post_outlives_its_authors_account()
        {
            var leaving = await factory.LoginAsNewStudent();
            var staying = await factory.LoginAsNewStudent();
            var admin = await factory.LoginAs(ApiFactory.AdminEmail);
            var topicId = await CreateTopic(leaving, title: "Ostaje");
            await CreateReply(staying, topicId, "Odgovor onoga tko ostaje");
            await CreateReply(leaving, topicId, "Odgovor onoga tko odlazi");

            var deleted = await leaving.DeleteAsync("/api/user/me");

            Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
            var topic = await Topic(staying, topicId);
            Assert.Equal("Ostaje", topic.GetProperty("title").GetString());
            Assert.Equal(JsonValueKind.Null, topic.GetProperty("authorName").ValueKind);
            Assert.Equal(JsonValueKind.Null, topic.GetProperty("authorRole").ValueKind);
            var replies = Replies(topic);
            Assert.Equal("Sara Student", replies[0].GetProperty("authorName").GetString());
            Assert.Equal("Odgovor onoga tko odlazi", replies[1].GetProperty("body").GetString());
            Assert.Equal(JsonValueKind.Null, replies[1].GetProperty("authorName").ValueKind);
            var listed = (await Topics(staying, Period)).GetProperty("topics").EnumerateArray().Single(t => t.GetProperty("id").GetInt32() == topicId);
            Assert.Equal(JsonValueKind.Null, listed.GetProperty("authorName").ValueKind);
            // Nobody's now: only an admin can remove it.
            Assert.False(replies[1].GetProperty("canDelete").GetBoolean());
            Assert.Equal(HttpStatusCode.Forbidden, (await staying.DeleteAsync($"/api/discussion/topic/{topicId}")).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/discussion/topic/{topicId}")).StatusCode);
        }

        [Fact]
        public async Task A_changed_name_and_role_show_on_posts_already_written()
        {
            var student = await factory.LoginAsNewStudent();
            var admin = await factory.LoginAs(ApiFactory.AdminEmail);
            var topicId = await CreateTopic(student);

            await student.PutAsJsonAsync("/api/user/me", new { firstName = "Mara", lastName = "Marić" });
            await admin.PutAsJsonAsync($"/api/user/{await IdOf(student)}/role", new { role = "teacher" });

            var topic = await Topic(student, topicId);
            Assert.Equal("Mara Marić", topic.GetProperty("authorName").GetString());
            Assert.Equal("teacher", topic.GetProperty("authorRole").GetString());
        }

        [Fact]
        public async Task The_sixth_post_in_a_minute_is_refused_and_reading_is_not_limited()
        {
            var student = await factory.LoginAsNewStudent();
            var other = await factory.LoginAsNewStudent();
            var topicId = await CreateTopic(student);
            for (var i = 0; i < 4; i++)
            {
                await CreateReply(student, topicId);
            }

            var sixth = await PostReply(student, topicId);

            Assert.Equal(HttpStatusCode.TooManyRequests, sixth.StatusCode);
            Assert.Equal(4, Replies(await Topic(student, topicId)).Length);
            for (var i = 0; i < 10; i++)
            {
                Assert.Equal(HttpStatusCode.OK, (await student.GetAsync($"/api/discussion/topic/{topicId}")).StatusCode);
            }
            // The limit is each account's own.
            Assert.Equal(HttpStatusCode.Created, (await PostReply(other, topicId)).StatusCode);
            // Deleting is not posting.
            Assert.Equal(HttpStatusCode.NotFound, (await student.DeleteAsync($"/api/discussion/reply/{NoSuchId}")).StatusCode);
        }
    }
}
