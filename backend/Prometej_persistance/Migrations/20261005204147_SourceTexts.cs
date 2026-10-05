using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Prometej_persistance.Migrations
{
    /// <inheritdoc />
    public partial class SourceTexts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SourceTextId",
                table: "Questions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceTextId",
                table: "Answers",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SourceTexts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    QuizId = table.Column<int>(type: "integer", nullable: false),
                    Caption = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    IsRetired = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourceTexts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SourceTexts_Quizzes_QuizId",
                        column: x => x.QuizId,
                        principalTable: "Quizzes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Questions_SourceTextId",
                table: "Questions",
                column: "SourceTextId");

            migrationBuilder.CreateIndex(
                name: "IX_Answers_SourceTextId",
                table: "Answers",
                column: "SourceTextId");

            migrationBuilder.CreateIndex(
                name: "IX_SourceTexts_QuizId",
                table: "SourceTexts",
                column: "QuizId");

            migrationBuilder.AddForeignKey(
                name: "FK_Answers_SourceTexts_SourceTextId",
                table: "Answers",
                column: "SourceTextId",
                principalTable: "SourceTexts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_SourceTexts_SourceTextId",
                table: "Questions",
                column: "SourceTextId",
                principalTable: "SourceTexts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Answers_SourceTexts_SourceTextId",
                table: "Answers");

            migrationBuilder.DropForeignKey(
                name: "FK_Questions_SourceTexts_SourceTextId",
                table: "Questions");

            migrationBuilder.DropTable(
                name: "SourceTexts");

            migrationBuilder.DropIndex(
                name: "IX_Questions_SourceTextId",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_Answers_SourceTextId",
                table: "Answers");

            migrationBuilder.DropColumn(
                name: "SourceTextId",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "SourceTextId",
                table: "Answers");
        }
    }
}
