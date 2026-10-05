using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prometej_persistance.Migrations
{
    /// <inheritdoc />
    public partial class QuestionOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Position",
                table: "Questions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Position",
                table: "Answers",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Position",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "Position",
                table: "Answers");
        }
    }
}
