using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prometej_persistance.Migrations
{
    /// <inheritdoc />
    public partial class PeriodCards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Periods",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Description", "Image" },
                values: new object[] { "Zaplovite u književnost stare Grčke i Rima: od Homerovih epova i Sofoklovih tragedija do Plautovih komedija i Vergilijeve Eneide.", "antika.webp" });

            migrationBuilder.UpdateData(
                table: "Periods",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Description", "Image" },
                values: new object[] { "Upoznajte najdulje razdoblje europske književnosti: junačke epove i viteške romane, crkvena prikazanja i prve hrvatske spomenike pisane glagoljicom.", "srednji-vijek.webp" });

            migrationBuilder.UpdateData(
                table: "Periods",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Description", "Image" },
                values: new object[] { "Otkrijte književnost koja u središte stavlja čovjeka: Marulićevu Juditu, Držićeve komedije, Shakespeareova Hamleta i Cervantesova Don Quijotea.", "renesansa.webp" });

            migrationBuilder.UpdateData(
                table: "Periods",
                keyColumn: "Id",
                keyValue: 4,
                column: "Image",
                value: "realizam.webp");

            migrationBuilder.UpdateData(
                table: "Periods",
                keyColumn: "Id",
                keyValue: 5,
                column: "Image",
                value: "romantizam.webp");

            migrationBuilder.UpdateData(
                table: "Periods",
                keyColumn: "Id",
                keyValue: 6,
                column: "Image",
                value: "modernizam.webp");

            migrationBuilder.UpdateData(
                table: "Periods",
                keyColumn: "Id",
                keyValue: 7,
                column: "Image",
                value: "humanizam-i-predrenesansa.webp");

            migrationBuilder.UpdateData(
                table: "Periods",
                keyColumn: "Id",
                keyValue: 8,
                column: "Image",
                value: "barok.webp");

            migrationBuilder.UpdateData(
                table: "Periods",
                keyColumn: "Id",
                keyValue: 9,
                column: "Image",
                value: "klasicizam.webp");

            migrationBuilder.UpdateData(
                table: "Periods",
                keyColumn: "Id",
                keyValue: 10,
                column: "Image",
                value: "predromantizam.webp");

            migrationBuilder.UpdateData(
                table: "Periods",
                keyColumn: "Id",
                keyValue: 11,
                column: "Image",
                value: "ekspresionizam.webp");

            migrationBuilder.UpdateData(
                table: "Periods",
                keyColumn: "Id",
                keyValue: 12,
                column: "Image",
                value: "suvremena-knjizevnost.webp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Periods",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Description", "Image" },
                values: new object[] { "Zaplovite u svijet davnina književnosti, od drevnih civilizacija antičke Grčke, drevnog Rima te najpoznatijih tragedija, komedija te epova", "antika.png" });

            migrationBuilder.UpdateData(
                table: "Periods",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Description", "Image" },
                values: new object[] { "Naučite više o mračnom i najduljem razdoblju književnosti, viteškim romanima, utjecaju crkve na književnost te najpoznatijim piscima tog doba", "srednji_vijek.png" });

            migrationBuilder.UpdateData(
                table: "Periods",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Description", "Image" },
                values: new object[] { "Otkrijte svijet renesansne književnosti, motiv čovjeka u središtu svemira, humanizam, te začeće romana kao književnog žanra", "renesansa.png" });

            migrationBuilder.UpdateData(
                table: "Periods",
                keyColumn: "Id",
                keyValue: 4,
                column: "Image",
                value: "realizam.png");

            migrationBuilder.UpdateData(
                table: "Periods",
                keyColumn: "Id",
                keyValue: 5,
                column: "Image",
                value: "romantizam.png");

            migrationBuilder.UpdateData(
                table: "Periods",
                keyColumn: "Id",
                keyValue: 6,
                column: "Image",
                value: "modernizam.png");

            migrationBuilder.UpdateData(
                table: "Periods",
                keyColumn: "Id",
                keyValue: 7,
                column: "Image",
                value: null);

            migrationBuilder.UpdateData(
                table: "Periods",
                keyColumn: "Id",
                keyValue: 8,
                column: "Image",
                value: null);

            migrationBuilder.UpdateData(
                table: "Periods",
                keyColumn: "Id",
                keyValue: 9,
                column: "Image",
                value: null);

            migrationBuilder.UpdateData(
                table: "Periods",
                keyColumn: "Id",
                keyValue: 10,
                column: "Image",
                value: null);

            migrationBuilder.UpdateData(
                table: "Periods",
                keyColumn: "Id",
                keyValue: 11,
                column: "Image",
                value: null);

            migrationBuilder.UpdateData(
                table: "Periods",
                keyColumn: "Id",
                keyValue: 12,
                column: "Image",
                value: null);
        }
    }
}
