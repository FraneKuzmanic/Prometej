using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prometej_persistance.Migrations
{
    /// <inheritdoc />
    public partial class QuizPeriod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PeriodId",
                table: "Quizzes",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_PeriodId",
                table: "Quizzes",
                column: "PeriodId");

            migrationBuilder.AddForeignKey(
                name: "FK_Quizzes_Periods_PeriodId",
                table: "Quizzes",
                column: "PeriodId",
                principalTable: "Periods",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Quizzes_Periods_PeriodId",
                table: "Quizzes");

            migrationBuilder.DropIndex(
                name: "IX_Quizzes_PeriodId",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "PeriodId",
                table: "Quizzes");
        }
    }
}
