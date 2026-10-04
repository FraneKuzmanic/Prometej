using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Prometej_persistance.Migrations
{
    /// <inheritdoc />
    public partial class Periods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Periods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    TimeFrame = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Image = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Periods", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Periods",
                columns: new[] { "Id", "Description", "Image", "Name", "SortOrder", "TimeFrame" },
                values: new object[,]
                {
                    { 1, "Zaplovite u svijet davnina književnosti, od drevnih civilizacija antičke Grčke, drevnog Rima te najpoznatijih tragedija, komedija te epova", "antika.png", "Antika", 1, "8. st. pr. Kr. – 5. st." },
                    { 2, "Naučite više o mračnom i najduljem razdoblju književnosti, viteškim romanima, utjecaju crkve na književnost te najpoznatijim piscima tog doba", "srednji_vijek.png", "Srednji vijek", 2, "5. – 15. st." },
                    { 3, "Otkrijte svijet renesansne književnosti, motiv čovjeka u središtu svemira, humanizam, te začeće romana kao književnog žanra", "renesansa.png", "Renesansa", 4, "15. – 16. st." },
                    { 4, "Realizam obilježava realan opis svijeta. Fokus je na dugačkom i detaljnom opisu likova i prostora što donosi brojne zanimljivosti iz tog vremena.", "realizam.png", "Realizam", 9, "druga polovica 19. st." },
                    { 5, "Osjetite romantizam, razdoblje koje obilježava osjećajnost, maštu, prirodu, fokus na pojedinca, njegovu impresiju i ekspresiju.", "romantizam.png", "Romantizam", 8, "kraj 18. – sredina 19. st." },
                    { 6, "Modernizam obilježava razdoblje estetike u književnosti, umjetnost radi same sebe. Zavirite u čari modernizma, njegove teme, motive i najpoznatije autore.", "modernizam.png", "Modernizam", 10, "sredina 19. – početak 20. st." },
                    { 7, "Upoznajte Dantea, Petrarcu i Boccaccia te humaniste koji su, vraćajući se antici, čovjeka ponovno stavili u središte zanimanja.", null, "Humanizam i predrenesansa", 3, "14. – 15. st." },
                    { 8, "Zavirite u književnost kićenoga stila, kontrasta i prolaznosti: od Gundulićeva Osmana i Dubravke do Calderónova kazališta.", null, "Barok", 5, "17. st." },
                    { 9, "Otkrijte doba razuma, reda i strogih pravila, Molièreovih komedija i prosvjetiteljskih ideja koje su mijenjale Europu.", null, "Klasicizam", 6, "17. – 18. st." },
                    { 10, "Pratite kako osjećaji potiskuju razum: Goetheov Werther, Sturm und Drang i prvi nagovještaji romantizma.", null, "Predromantizam", 7, "druga polovica 18. st." },
                    { 11, "Uđite u književnost krika, pobune i slobodnoga stiha, uz A. B. Šimića, Kafku i ranoga Krležu.", null, "Ekspresionizam", 11, "1910. – 1925." },
                    { 12, "Istražite književnost nakon ekspresionizma: Krležine Glembajeve, egzistencijalizam i Camusova Stranca, Marinkovićeva Kiklopa i postmodernizam.", null, "Suvremena književnost", 12, "od kraja 1920-ih do danas" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PeriodContents_PeriodId",
                table: "PeriodContents",
                column: "PeriodId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PeriodContents_Periods_PeriodId",
                table: "PeriodContents",
                column: "PeriodId",
                principalTable: "Periods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PeriodContents_Periods_PeriodId",
                table: "PeriodContents");

            migrationBuilder.DropTable(
                name: "Periods");

            migrationBuilder.DropIndex(
                name: "IX_PeriodContents_PeriodId",
                table: "PeriodContents");
        }
    }
}
