using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Prometej_tests
{
    // The seeded admin acts; every account whose role changes is a student the test registered.
    public class UserRoleTests(ApiFactory factory) : IClassFixture<ApiFactory>
    {
        private static Task<JsonElement> Me(HttpClient client) =>
            client.GetFromJsonAsync<JsonElement>("/api/user/me");

        private static async Task<int> IdOf(HttpClient client) =>
            (await Me(client)).GetProperty("id").GetInt32();

        private static async Task<string?> RoleOf(HttpClient client) =>
            (await Me(client)).GetProperty("role").GetString();

        private static Task<HttpResponseMessage> SetRole(HttpClient admin, int id, string role) =>
            admin.PutAsJsonAsync($"/api/user/{id}/role", new { role });

        private static async Task<JsonElement[]> Users(HttpClient admin) =>
            (await admin.GetFromJsonAsync<JsonElement>("/api/user")).EnumerateArray().ToArray();

        private static async Task<int> CreateQuiz(HttpClient client)
        {
            var body = new
            {
                quiz = new { title = "Kviz", isPrivate = false },
                questions = new[]
                {
                    new { questionTitle = "Pitanje", firstAnswer = "A", secondAnswer = "B", thirdAnswer = "C", fourthAnswer = "D", correctOption = 1 },
                },
            };
            var response = await client.PostAsJsonAsync("/api/quiz/create", body);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return await response.Content.ReadFromJsonAsync<int>();
        }

        [Fact]
        public async Task The_user_list_is_for_admins_only()
        {
            var anonymous = factory.CreateHttpsClient();
            var student = await factory.LoginAsNewStudent();
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var admin = await factory.LoginAs(ApiFactory.AdminEmail);

            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/user")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await student.GetAsync("/api/user")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await teacher.GetAsync("/api/user")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/user")).StatusCode);
        }

        [Fact]
        public async Task The_user_list_has_each_account_with_its_quiz_count_and_no_password_or_stamp()
        {
            var admin = await factory.LoginAs(ApiFactory.AdminEmail);
            var user = await factory.LoginAsNewStudent();
            var me = await Me(user);
            var id = me.GetProperty("id").GetInt32();
            await SetRole(admin, id, "teacher");
            await CreateQuiz(user);

            var response = await admin.GetAsync("/api/user");

            var json = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("stamp", json, StringComparison.OrdinalIgnoreCase);
            var users = JsonDocument.Parse(json).RootElement.EnumerateArray().ToArray();
            var row = users.Single(u => u.GetProperty("id").GetInt32() == id);
            Assert.Equal(1, row.GetProperty("quizCount").GetInt32());
            Assert.Equal("teacher", row.GetProperty("role").GetString());
            Assert.Equal(me.GetProperty("email").GetString(), row.GetProperty("email").GetString());
            Assert.Equal("Sara", row.GetProperty("firstName").GetString());
            Assert.Equal("Student", row.GetProperty("lastName").GetString());
            Assert.Contains(users, u => u.GetProperty("email").GetString() == ApiFactory.AdminEmail);
        }

        [Fact]
        public async Task A_role_change_holds_at_once_for_a_session_that_is_already_open()
        {
            var admin = await factory.LoginAs(ApiFactory.AdminEmail);
            var user = await factory.LoginAsNewStudent();
            var id = await IdOf(user);
            Assert.Equal(HttpStatusCode.Forbidden, (await user.GetAsync("/api/quiz/getMyQuizzes")).StatusCode);

            // The same client throughout: it never signs in again.
            Assert.Equal(HttpStatusCode.NoContent, (await SetRole(admin, id, "teacher")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await user.GetAsync("/api/quiz/getMyQuizzes")).StatusCode);
            Assert.Equal("teacher", await RoleOf(user));

            Assert.Equal(HttpStatusCode.NoContent, (await SetRole(admin, id, "admin")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await user.GetAsync("/api/user")).StatusCode);

            Assert.Equal(HttpStatusCode.NoContent, (await SetRole(admin, id, "student")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await user.GetAsync("/api/quiz/getMyQuizzes")).StatusCode);
        }

        [Fact]
        public async Task An_admin_cannot_change_their_own_role()
        {
            var admin = await factory.LoginAs(ApiFactory.AdminEmail);
            var id = await IdOf(admin);

            var toTeacher = await SetRole(admin, id, "teacher");
            var toAdmin = await SetRole(admin, id, "admin");

            Assert.Equal(HttpStatusCode.Forbidden, toTeacher.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, toAdmin.StatusCode);
            Assert.Equal("admin", await RoleOf(admin));
        }

        [Fact]
        public async Task A_creator_with_quizzes_cannot_be_made_a_student()
        {
            var admin = await factory.LoginAs(ApiFactory.AdminEmail);
            var user = await factory.LoginAsNewStudent();
            var id = await IdOf(user);
            await SetRole(admin, id, "teacher");
            var quizId = await CreateQuiz(user);

            var refused = await SetRole(admin, id, "student");

            Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
            Assert.Equal("teacher", await RoleOf(user));
            // Between teacher and admin the quizzes keep a creator who can edit them.
            Assert.Equal(HttpStatusCode.NoContent, (await SetRole(admin, id, "admin")).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await SetRole(admin, id, "teacher")).StatusCode);

            await user.DeleteAsync($"/api/quiz/delete/{quizId}");
            var allowed = await SetRole(admin, id, "student");

            Assert.Equal(HttpStatusCode.NoContent, allowed.StatusCode);
            Assert.Equal("student", await RoleOf(user));
        }

        [Fact]
        public async Task An_unknown_role_or_user_is_refused()
        {
            var admin = await factory.LoginAs(ApiFactory.AdminEmail);
            var id = await IdOf(await factory.LoginAsNewStudent());

            foreach (var role in new[] { "Admin", "superuser", "" })
            {
                Assert.Equal(HttpStatusCode.BadRequest, (await SetRole(admin, id, role)).StatusCode);
            }

            var noRole = await admin.PutAsJsonAsync($"/api/user/{id}/role", new { });
            var unknownUser = await SetRole(admin, 999999, "teacher");

            Assert.Equal(HttpStatusCode.BadRequest, noRole.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, unknownUser.StatusCode);
        }

        [Fact]
        public async Task Setting_the_role_a_user_already_has_changes_nothing()
        {
            var admin = await factory.LoginAs(ApiFactory.AdminEmail);
            var user = await factory.LoginAsNewStudent();

            var response = await SetRole(admin, await IdOf(user), "student");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal("student", await RoleOf(user));
        }

        [Fact]
        public async Task Only_an_admin_changes_a_role()
        {
            var anonymous = factory.CreateHttpsClient();
            var teacher = await factory.LoginAs(ApiFactory.TeacherEmail);
            var student = await factory.LoginAsNewStudent();
            var id = await IdOf(student);

            Assert.Equal(HttpStatusCode.Unauthorized, (await SetRole(anonymous, id, "teacher")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await SetRole(teacher, id, "teacher")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await SetRole(student, id, "admin")).StatusCode);
            Assert.Equal("student", await RoleOf(student));
        }
    }
}
