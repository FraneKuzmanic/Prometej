using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Prometej_persistance;
using Testcontainers.PostgreSql;

namespace Prometej_tests
{
    // Proves the QuizIntegrity migration on data shaped like what it will meet: correct
    // answers stored as text, entry codes on public quizzes and one code on two quizzes.
    public class QuizIntegrityMigrationTests : IAsyncLifetime
    {
        private const string LastMigrationBefore = "20261001195646_AuthHardening";
        private const string QuizIntegrity = "20261001231509_QuizIntegrity";

        private readonly PostgreSqlContainer _db = new PostgreSqlBuilder("postgres:15-alpine").Build();

        public Task InitializeAsync() => _db.StartAsync();

        public Task DisposeAsync() => _db.DisposeAsync().AsTask();

        private DataContext CreateContext() =>
            new(new DbContextOptionsBuilder<DataContext>().UseNpgsql(_db.GetConnectionString()).Options);

        private record QuizRow(int Id, bool IsPrivate, int? EntryCode);

        private static List<QuizRow> ReadQuizzes(DataContext context) =>
            context.Quizzes.AsNoTracking().OrderBy(q => q.Id)
                .Select(q => new QuizRow(q.Id, q.IsPrivate, q.EntryCode)).ToList();

        private static Dictionary<string, int?> ReadCorrectOptions(DataContext context) =>
            context.Questions.AsNoTracking().Select(q => new { q.QuestionTitle, q.CorrectOption })
                .ToDictionary(q => q.QuestionTitle, q => q.CorrectOption);

        [Fact]
        public async Task Migration_turns_correct_answers_into_option_numbers_and_makes_entry_codes_unique()
        {
            await using var context = CreateContext();
            var migrator = context.GetService<IMigrator>();

            await migrator.MigrateAsync(LastMigrationBefore);
            await context.Database.ExecuteSqlRawAsync("""
                INSERT INTO "Users" ("Id", "FirstName", "LastName", "Email", "PasswordHash", "Role") VALUES
                  (1, 'Tea', 'Teacher', 'tea@example.com', 'x', 'teacher');
                INSERT INTO "Quizzes" ("Id", "Title", "IsPrivate", "CreatorId", "EntryCode") VALUES
                  (1, 'Keeps its code', true, 1, 12345),
                  (2, 'Shares a code', true, 1, 12345),
                  (3, 'Public with a code', false, 1, 123456),
                  (4, 'Private without a code', true, 1, NULL),
                  (5, 'Private with six digits', true, 1, 654321);
                INSERT INTO "Questions" ("QuizId", "QuestionTitle", "FirstAnswer", "SecondAnswer", "ThirdAnswer", "FourthAnswer", "CorrectAnswer") VALUES
                  (1, 'third', 'A', 'B', 'C', 'D', 'C'),
                  (1, 'fourth', 'A', 'B', 'C', 'D', 'D'),
                  (1, 'no match', 'A', 'B', 'C', 'D', 'Z'),
                  (1, 'empty', 'A', 'B', 'C', 'D', ''),
                  (1, 'second of two equal', 'A', 'B', 'B', 'D', 'B');
                """);

            await migrator.MigrateAsync(QuizIntegrity);

            var options = ReadCorrectOptions(context);
            Assert.Equal(3, options["third"]);
            Assert.Equal(4, options["fourth"]);
            Assert.Equal(1, options["no match"]);
            Assert.Equal(1, options["empty"]);
            Assert.Equal(2, options["second of two equal"]);

            var quizzes = ReadQuizzes(context);
            Assert.Equal(12345, quizzes[0].EntryCode);
            Assert.Null(quizzes[2].EntryCode);
            var codes = quizzes.Where(q => q.IsPrivate).Select(q => q.EntryCode).ToList();
            Assert.All(codes, code => Assert.InRange(code!.Value, 10000, 99999));
            Assert.Equal(4, codes.Distinct().Count());

            await migrator.MigrateAsync();

            Assert.Equal(quizzes, ReadQuizzes(context));
            Assert.Equal(options, ReadCorrectOptions(context));
        }

        [Fact]
        public async Task Migration_runs_on_an_empty_database()
        {
            await using var context = CreateContext();

            await context.GetService<IMigrator>().MigrateAsync();

            Assert.Empty(ReadQuizzes(context));
        }
    }
}
