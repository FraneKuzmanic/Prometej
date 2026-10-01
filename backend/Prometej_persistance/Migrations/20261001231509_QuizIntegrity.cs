using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prometej_persistance.Migrations
{
    /// <inheritdoc />
    public partial class QuizIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CorrectOption",
                table: "Questions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // The correct answer was a copy of an option's text. It becomes the number of the
            // first option with that text; a question whose copy matches no option gets 1.
            // This runs before the old column is dropped, which is not the scaffolded order.
            migrationBuilder.Sql("""
                UPDATE "Questions"
                   SET "CorrectOption" = CASE
                         WHEN "CorrectAnswer" = "FirstAnswer" THEN 1
                         WHEN "CorrectAnswer" = "SecondAnswer" THEN 2
                         WHEN "CorrectAnswer" = "ThirdAnswer" THEN 3
                         WHEN "CorrectAnswer" = "FourthAnswer" THEN 4
                         ELSE 1
                       END;
                """);

            migrationBuilder.DropColumn(
                name: "CorrectAnswer",
                table: "Questions");

            // Entry codes came from the browser and nothing kept them unique. Before the unique
            // index goes on: a public quiz loses its code, and a private quiz gets a new unused
            // one if it has none, has one that is not five digits, or shares it with an older quiz.
            migrationBuilder.Sql("""
                UPDATE "Quizzes" SET "EntryCode" = NULL WHERE NOT "IsPrivate";

                DO $$
                DECLARE
                  r RECORD;
                  candidate integer;
                BEGIN
                  FOR r IN SELECT q."Id" FROM "Quizzes" q
                            WHERE q."IsPrivate"
                              AND (q."EntryCode" IS NULL
                                   OR q."EntryCode" NOT BETWEEN 10000 AND 99999
                                   OR EXISTS (SELECT 1 FROM "Quizzes" o
                                               WHERE o."EntryCode" = q."EntryCode" AND o."Id" < q."Id"))
                            ORDER BY q."Id"
                  LOOP
                    LOOP
                      candidate := 10000 + floor(random() * 90000)::int;
                      EXIT WHEN NOT EXISTS (SELECT 1 FROM "Quizzes" WHERE "EntryCode" = candidate);
                    END LOOP;
                    UPDATE "Quizzes" SET "EntryCode" = candidate WHERE "Id" = r."Id";
                  END LOOP;
                END $$;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_EntryCode",
                table: "Quizzes",
                column: "EntryCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Entry codes that were removed or reassigned stay as they are.
            migrationBuilder.DropIndex(
                name: "IX_Quizzes_EntryCode",
                table: "Quizzes");

            migrationBuilder.AddColumn<string>(
                name: "CorrectAnswer",
                table: "Questions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE "Questions"
                   SET "CorrectAnswer" = CASE "CorrectOption"
                         WHEN 2 THEN "SecondAnswer"
                         WHEN 3 THEN "ThirdAnswer"
                         WHEN 4 THEN "FourthAnswer"
                         ELSE "FirstAnswer"
                       END;
                """);

            migrationBuilder.DropColumn(
                name: "CorrectOption",
                table: "Questions");
        }
    }
}
