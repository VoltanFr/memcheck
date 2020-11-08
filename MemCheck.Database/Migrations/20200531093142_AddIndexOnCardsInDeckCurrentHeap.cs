using Microsoft.EntityFrameworkCore.Migrations;

namespace MemCheck.Database.Migrations
{
    public partial class AddIndexOnCardsInDeckCurrentHeap : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_CardsInDecks_CurrentHeap",
                table: "CardsInDecks",
                column: "CurrentHeap");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CardsInDecks_CurrentHeap",
                table: "CardsInDecks");
        }
    }
}
