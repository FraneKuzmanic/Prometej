using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Prometej_persistance;
using Testcontainers.PostgreSql;

namespace Prometej_tests
{
    // Proves the PlayHistory migration on answers stored before it: each gets the title and
    // the explanation of its question, also when that question has been retired.
    public class PlayHistoryMigrationTests : IAsyncLifetime
    {
        private const string LastMigrationBefore = "20261004231217_QuizPeriod";
        private const string PlayHistory = "20261005073930_PlayHistory";

        private readonly PostgreSqlContainer _db = new PostgreSqlBuilder("postgres:15-alpine").Build();

        public Task InitializeAsync() => _db.StartAsync();

        public Task DisposeAsync() => _db.DisposeAsync().AsTask();

        private DataContext CreateContext() =>
            new(new DbContextOptionsBuilder<DataContext>().UseNpgsql(_db.GetConnectionString()).Options);

        private record AnswerRow(int QuestionId, string QuestionTitle, string? ExploreMore);

        private static List<AnswerRow> ReadAnswers(DataContext context) =>
            context.Answers.AsNoTracking().OrderBy(a => a.QuestionId)
                .Select(a => new AnswerRow(a.QuestionId, a.QuestionTitle, a.ExploreMore)).ToList();

        private static List<Guid?> ReadSubmissionKeys(DataContext context) =>
            context.QuizGames.AsNoTracking().Select(g => g.SubmissionKey).ToList();

        [Fact]
        public async Task Migration_gives_stored_answers_their_questions_title_and_explore_more()
        {
            await using var context = CreateContext();
            var migrator = context.GetService<IMigrator>();

            await migrator.MigrateAsync(LastMigrationBefore);
            await context.Database.ExecuteSqlRawAsync("""
                INSERT INTO "Users" ("Id", "FirstName", "LastName", "Email", "PasswordHash", "Role") VALUES
                  (1, 'Tea', 'Teacher', 'tea@example.com', 'x', 'teacher'),
                  (2, 'Sara', 'Student', 'sara@example.com', 'x', 'student');
                INSERT INTO "Quizzes" ("Id", "Title", "IsPrivate", "CreatorId") VALUES
                  (1, 'Kviz', false, 1);
                INSERT INTO "Questions" ("Id", "QuizId", "QuestionTitle", "FirstAnswer", "SecondAnswer", "ThirdAnswer", "FourthAnswer", "CorrectOption", "ExploreMore", "IsRetired") VALUES
                  (1, 1, 'S objašnjenjem', 'A', 'B', 'C', 'D', 1, 'Objašnjenje', false),
                  (2, 1, 'S praznim objašnjenjem', 'A', 'B', 'C', 'D', 1, '', true),
                  (3, 1, 'Bez objašnjenja', 'A', 'B', 'C', 'D', 1, NULL, false);
                INSERT INTO "QuizGames" ("Id", "QuizId", "UserId", "UserName", "Score", "DatePlayed") VALUES
                  (1, 1, 2, 'Sara Student', 3, '2026-10-01T10:00:00Z');
                INSERT INTO "Answers" ("QuizGameId", "QuestionId", "AnswerText", "CorrectAnswer") VALUES
                  (1, 1, 'A', 'A'),
                  (1, 2, 'A', 'A'),
                  (1, 3, 'A', 'A');
                """);

            await migrator.MigrateAsync(PlayHistory);

            var answers = ReadAnswers(context);
            Assert.Equal(
                [
                    new AnswerRow(1, "S objašnjenjem", "Objašnjenje"),
                    new AnswerRow(2, "S praznim objašnjenjem", null),
                    new AnswerRow(3, "Bez objašnjenja", null),
                ],
                answers);
            Assert.Equal([null], ReadSubmissionKeys(context));

            await migrator.MigrateAsync();

            Assert.Equal(answers, ReadAnswers(context));
            Assert.Equal([null], ReadSubmissionKeys(context));
        }

        [Fact]
        public async Task Migration_runs_on_an_empty_database()
        {
            await using var context = CreateContext();

            await context.GetService<IMigrator>().MigrateAsync();

            Assert.Empty(ReadAnswers(context));
        }
    }
}
