using Microsoft.EntityFrameworkCore.Migrations;

namespace MemCheck.Database.Migrations;

public partial class NewIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_UserCardRatings_UserId",
            table: "UserCardRatings");

        migrationBuilder.DropIndex(
            name: "IX_CardsInDecks_DeckId",
            table: "CardsInDecks");

        migrationBuilder.CreateIndex(
            name: "IX_UserCardRatings_UserId",
            table: "UserCardRatings",
            column: "UserId")
            .Annotation("SqlServer:Include", new[] { "Rating" });

        migrationBuilder.CreateIndex(
            name: "IX_CardsInDecks_DeckId_CurrentHeap_CardId",
            table: "CardsInDecks",
            columns: new[] { "DeckId", "CurrentHeap", "CardId" })
            .Annotation("SqlServer:Include", new[] { "AddToDeckUtcTime", "BiggestHeapReached", "LastLearnUtcTime", "NbTimesInNotLearnedHeap" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_UserCardRatings_UserId",
            table: "UserCardRatings");

        migrationBuilder.DropIndex(
            name: "IX_CardsInDecks_DeckId_CurrentHeap_CardId",
            table: "CardsInDecks");

        migrationBuilder.CreateIndex(
            name: "IX_UserCardRatings_UserId",
            table: "UserCardRatings",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_CardsInDecks_DeckId",
            table: "CardsInDecks",
            column: "DeckId");
    }
}
