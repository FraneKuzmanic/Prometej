using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Prometej_persistance;
using Testcontainers.PostgreSql;

namespace Prometej_tests
{
    // The three migrations of the question types move no data. This shows what that means for
    // a quiz and a play stored before them: the questions are choice questions in the order
    // they had, and the play keeps its score and its rows.
    public class QuestionTypesMigrationTests : IAsyncLifetime
    {
        private const string LastMigrationBefore = "20261005184230_PeriodCards";

        private readonly PostgreSqlContainer _db = new PostgreSqlBuilder("postgres:15-alpine").Build();

        public Task InitializeAsync() => _db.StartAsync();

        public Task DisposeAsync() => _db.DisposeAsync().AsTask();

        private DataContext CreateContext() =>
            new(new DbContextOptionsBuilder<DataContext>().UseNpgsql(_db.GetConnectionString()).Options);

        private record QuestionRow(string Title, string Type, int Position, bool HasContent, string Options, int? CorrectOption, int? SourceTextId);

        private record AnswerRow(int QuestionId, string AnswerText, string CorrectAnswer, int Position, string? Item, int? Place, int? SourceTextId);

        [Fact]
        public async Task Questions_and_answers_stored_before_the_question_types_read_the_same_after_them()
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
                INSERT INTO "Questions" ("Id", "QuizId", "QuestionTitle", "FirstAnswer", "SecondAnswer", "ThirdAnswer", "FourthAnswer", "CorrectOption", "IsRetired") VALUES
                  (1, 1, 'Prvo', 'A', 'B', 'C', 'D', 1, false),
                  (2, 1, 'Drugo', 'E', 'F', 'G', 'H', 2, false),
                  (3, 1, 'Treće', 'I', 'J', 'K', 'L', 3, false);
                INSERT INTO "QuizGames" ("Id", "QuizId", "UserId", "UserName", "Score", "DatePlayed") VALUES
                  (1, 1, 2, 'Sara Student', 2, '2026-10-01T10:00:00Z');
                INSERT INTO "Answers" ("QuizGameId", "QuestionId", "QuestionTitle", "AnswerText", "CorrectAnswer") VALUES
                  (1, 1, 'Prvo', 'A', 'A'),
                  (1, 2, 'Drugo', 'F', 'F'),
                  (1, 3, 'Treće', 'I', 'K');
                """);

            await migrator.MigrateAsync();

            // In the order the quiz is read in: by position, then by id.
            var questions = context.Questions.AsNoTracking().OrderBy(q => q.Position).ThenBy(q => q.Id)
                .Select(q => new QuestionRow(q.QuestionTitle, q.Type, q.Position, q.Content != null,
                    q.FirstAnswer + q.SecondAnswer + q.ThirdAnswer + q.FourthAnswer, q.CorrectOption, q.SourceTextId))
                .ToList();
            Assert.Equal(
                [
                    new QuestionRow("Prvo", "choice", 0, false, "ABCD", 1, null),
                    new QuestionRow("Drugo", "choice", 0, false, "EFGH", 2, null),
                    new QuestionRow("Treće", "choice", 0, false, "IJKL", 3, null),
                ],
                questions);

            // In the order a review reads a play in.
            var answers = context.Answers.AsNoTracking().OrderBy(a => a.Position).ThenBy(a => a.QuestionId).ThenBy(a => a.Id)
                .Select(a => new AnswerRow(a.QuestionId, a.AnswerText, a.CorrectAnswer, a.Position, a.Item, a.Place, a.SourceTextId))
                .ToList();
            Assert.Equal(
                [
                    new AnswerRow(1, "A", "A", 0, null, null, null),
                    new AnswerRow(2, "F", "F", 0, null, null, null),
                    new AnswerRow(3, "I", "K", 0, null, null, null),
                ],
                answers);

            // The score is still two of three: a row is a point, and no row was added or lost.
            var game = context.QuizGames.AsNoTracking().Select(g => new { g.Score, MaxScore = g.Answers.Count }).Single();
            Assert.Equal(2, game.Score);
            Assert.Equal(3, game.MaxScore);
            Assert.Empty(context.SourceTexts);
        }

        [Fact]
        public async Task Migration_runs_on_an_empty_database()
        {
            await using var context = CreateContext();

            await context.GetService<IMigrator>().MigrateAsync();

            Assert.Empty(context.Questions.Select(q => q.Type));
        }
    }
}
