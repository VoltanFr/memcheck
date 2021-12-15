using Microsoft.EntityFrameworkCore.Migrations;

namespace MemCheck.Database.Migrations
{
    public partial class AddCascadeBehaviorForCardInDecks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CardsInDecks_Cards_CardId",
                table: "CardsInDecks");

            migrationBuilder.AddForeignKey(
                name: "FK_CardsInDecks_Cards_CardId",
                table: "CardsInDecks",
                column: "CardId",
                principalTable: "Cards",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CardsInDecks_Cards_CardId",
                table: "CardsInDecks");

            migrationBuilder.AddForeignKey(
                name: "FK_CardsInDecks_Cards_CardId",
                table: "CardsInDecks",
                column: "CardId",
                principalTable: "Cards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
