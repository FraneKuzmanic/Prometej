using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Prometej_persistance;
using Testcontainers.PostgreSql;

namespace Prometej_tests
{
    // Proves the Periods migration on a database that already holds Period Content: the six
    // Periods that existed as bare numbers keep their content, and their ids are not the order.
    public class PeriodsMigrationTests : IAsyncLifetime
    {
        private const string LastMigrationBefore = "20261002152113_Unaccent";
        private const string Periods = "20261002160313_Periods";

        private static readonly string[] CurriculumOrder =
        [
            "Antika", "Srednji vijek", "Humanizam i predrenesansa", "Renesansa", "Barok", "Klasicizam",
            "Predromantizam", "Romantizam", "Realizam", "Modernizam", "Ekspresionizam", "Suvremena književnost",
        ];

        private readonly PostgreSqlContainer _db = new PostgreSqlBuilder("postgres:15-alpine").Build();

        public Task InitializeAsync() => _db.StartAsync();

        public Task DisposeAsync() => _db.DisposeAsync().AsTask();

        private DataContext CreateContext() =>
            new(new DbContextOptionsBuilder<DataContext>().UseNpgsql(_db.GetConnectionString()).Options);

        private static List<string> ReadPeriodNames(DataContext context) =>
            context.Periods.AsNoTracking().OrderBy(p => p.SortOrder).Select(p => p.Name).ToList();

        private static Dictionary<string, string> ReadContentByPeriodName(DataContext context) =>
            context.PeriodContents.AsNoTracking().Select(c => new { c.Period!.Name, c.Content })
                .ToDictionary(c => c.Name, c => c.Content);

        [Fact]
        public async Task Migration_keeps_each_content_with_its_period()
        {
            await using var context = CreateContext();
            var migrator = context.GetService<IMigrator>();

            await migrator.MigrateAsync(LastMigrationBefore);
            await context.Database.ExecuteSqlRawAsync("""
                INSERT INTO "PeriodContents" ("PeriodId", "Content") VALUES
                  (1, '<p>Antika</p>'),
                  (2, '<p>Srednji vijek</p>'),
                  (3, '<p>Renesansa</p>'),
                  (4, '<p>Realizam</p>'),
                  (5, '<p>Romantizam</p>'),
                  (6, '<p>Modernizam</p>');
                """);

            await migrator.MigrateAsync(Periods);

            var contents = ReadContentByPeriodName(context);
            Assert.Equal(6, contents.Count);
            Assert.All(contents, content => Assert.Equal($"<p>{content.Key}</p>", content.Value));
            Assert.Equal(CurriculumOrder, ReadPeriodNames(context));

            await migrator.MigrateAsync();

            Assert.Equal(contents, ReadContentByPeriodName(context));
            Assert.Equal(CurriculumOrder, ReadPeriodNames(context));
        }

        [Fact]
        public async Task A_period_has_one_content_and_a_content_has_a_period()
        {
            await using var context = CreateContext();
            await context.GetService<IMigrator>().MigrateAsync();
            await context.Database.ExecuteSqlRawAsync(
                """INSERT INTO "PeriodContents" ("PeriodId", "Content") VALUES (1, '<p>Antika</p>');""");

            var second = await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlRawAsync(
                """INSERT INTO "PeriodContents" ("PeriodId", "Content") VALUES (1, '<p>Opet antika</p>');"""));
            var unknown = await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlRawAsync(
                """INSERT INTO "PeriodContents" ("PeriodId", "Content") VALUES (99, '<p>Nema ga</p>');"""));

            Assert.Equal(PostgresErrorCodes.UniqueViolation, second.SqlState);
            Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, unknown.SqlState);
        }

        [Fact]
        public async Task Migration_runs_on_an_empty_database()
        {
            await using var context = CreateContext();

            await context.GetService<IMigrator>().MigrateAsync();

            Assert.Equal(CurriculumOrder, ReadPeriodNames(context));
            Assert.Empty(ReadContentByPeriodName(context));
        }
    }
}
