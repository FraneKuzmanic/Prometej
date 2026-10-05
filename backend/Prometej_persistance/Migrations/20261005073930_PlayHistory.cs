using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prometej_persistance.Migrations
{
    /// <inheritdoc />
    public partial class PlayHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_QuizGames_UserId",
                table: "QuizGames");

            migrationBuilder.AddColumn<Guid>(
                name: "SubmissionKey",
                table: "QuizGames",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExploreMore",
                table: "Answers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QuestionTitle",
                table: "Answers",
                type: "text",
                nullable: false,
                defaultValue: "");

            // The answers stored so far get their question's title and explore more text as they
            // are now: what they were when played was never kept. An empty text is none.
            migrationBuilder.Sql("""
                UPDATE "Answers" a
                   SET "QuestionTitle" = q."QuestionTitle",
                       "ExploreMore" = NULLIF(btrim(q."ExploreMore"), '')
                  FROM "Questions" q
                 WHERE q."Id" = a."QuestionId";
                """);

            migrationBuilder.CreateIndex(
                name: "IX_QuizGames_UserId_SubmissionKey",
                table: "QuizGames",
                columns: new[] { "UserId", "SubmissionKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_QuizGames_UserId_SubmissionKey",
                table: "QuizGames");

            migrationBuilder.DropColumn(
                name: "SubmissionKey",
                table: "QuizGames");

            migrationBuilder.DropColumn(
                name: "ExploreMore",
                table: "Answers");

            migrationBuilder.DropColumn(
                name: "QuestionTitle",
                table: "Answers");

            migrationBuilder.CreateIndex(
                name: "IX_QuizGames_UserId",
                table: "QuizGames",
                column: "UserId");
        }
    }
}
