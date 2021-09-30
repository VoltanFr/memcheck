using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MemCheck.Database.Migrations
{
    public partial class AddIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_CardsInDecks_DeckId_CurrentHeap_ExpiryUtcTime",
                table: "CardsInDecks",
                columns: new[] { "DeckId", "CurrentHeap", "ExpiryUtcTime" })
                .Annotation("SqlServer:Include", new[] { "AddToDeckUtcTime", "BiggestHeapReached", "CardId", "LastLearnUtcTime", "NbTimesInNotLearnedHeap" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CardsInDecks_DeckId_CurrentHeap_ExpiryUtcTime",
                table: "CardsInDecks");
        }
    }
}
