using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Prometej_core.Auth;
using Prometej_persistance;
using Testcontainers.PostgreSql;

namespace Prometej_tests
{
    // Proves the AuthHardening migration on a database shaped like the one it will meet:
    // plain-text passwords and two accounts sharing an email. Uses its own container
    // because ApiFactory migrates straight to the latest schema.
    public class LegacyPasswordMigrationTests : IAsyncLifetime
    {
        private const string LastMigrationBeforeAuth = "20240610142217_answers-quiz-game-added";

        private readonly PostgreSqlContainer _db = new PostgreSqlBuilder("postgres:15-alpine").Build();

        public Task InitializeAsync() => _db.StartAsync();

        public Task DisposeAsync() => _db.DisposeAsync().AsTask();

        private DataContext CreateContext() =>
            new(new DbContextOptionsBuilder<DataContext>().UseNpgsql(_db.GetConnectionString()).Options);

        private record UserRow(int Id, string Email, string PasswordHash);

        private static List<UserRow> ReadUsers(DataContext context) =>
            context.Users.AsNoTracking().OrderBy(u => u.Id)
                .Select(u => new UserRow(u.Id, u.Email, u.PasswordHash)).ToList();

        [Fact]
        public async Task Migration_hashes_plain_text_passwords_and_separates_duplicate_emails()
        {
            await using var context = CreateContext();
            var migrator = context.GetService<IMigrator>();

            await migrator.MigrateAsync(LastMigrationBeforeAuth);
            await context.Database.ExecuteSqlRawAsync("""
                INSERT INTO "Users" ("FirstName", "LastName", "Email", "Password", "Role") VALUES
                  ('Iva', 'First', 'Iva@Example.com', 'plain-one', 'teacher'),
                  ('Iva', 'Second', ' iva@example.com', 'plain-two', 'student');
                """);

            await migrator.MigrateAsync();

            var users = ReadUsers(context);
            Assert.Equal(2, users.Count);
            Assert.Equal("iva@example.com", users[0].Email);
            Assert.Equal($"iva@example.com.duplicate-{users[1].Id}", users[1].Email);
            Assert.StartsWith("$2", users[0].PasswordHash);
            Assert.True(PasswordHasher.Verify("plain-one", users[0].PasswordHash));
            Assert.True(PasswordHasher.Verify("plain-two", users[1].PasswordHash));
            Assert.False(PasswordHasher.Verify("plain-two", users[0].PasswordHash));

            await migrator.MigrateAsync();

            Assert.Equal(users, ReadUsers(context));
        }

        [Fact]
        public async Task Migration_runs_on_an_empty_database_without_pgcrypto()
        {
            await using var context = CreateContext();

            await context.GetService<IMigrator>().MigrateAsync();

            var extensions = await context.Database
                .SqlQueryRaw<string>("SELECT extname AS \"Value\" FROM pg_extension").ToListAsync();
            Assert.DoesNotContain("pgcrypto", extensions);
        }
    }
}
