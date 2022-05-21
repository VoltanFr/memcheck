using Microsoft.EntityFrameworkCore.Migrations;

namespace MemCheck.Database.Migrations;

public partial class RenameInSearchSubscription : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_CardInSearchResult_SearchSubscriptions_SearchSubscriptionId",
            table: "CardInSearchResult");

        migrationBuilder.DropPrimaryKey(
            name: "PK_CardInSearchResult",
            table: "CardInSearchResult");

        migrationBuilder.RenameTable(
            name: "CardInSearchResult",
            newName: "CardsInSearchResults");

        migrationBuilder.AddPrimaryKey(
            name: "PK_CardsInSearchResults",
            table: "CardsInSearchResults",
            columns: new[] { "SearchSubscriptionId", "CardId" });

        migrationBuilder.AddForeignKey(
            name: "FK_CardsInSearchResults_SearchSubscriptions_SearchSubscriptionId",
            table: "CardsInSearchResults",
            column: "SearchSubscriptionId",
            principalTable: "SearchSubscriptions",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_CardsInSearchResults_SearchSubscriptions_SearchSubscriptionId",
            table: "CardsInSearchResults");

        migrationBuilder.DropPrimaryKey(
            name: "PK_CardsInSearchResults",
            table: "CardsInSearchResults");

        migrationBuilder.RenameTable(
            name: "CardsInSearchResults",
            newName: "CardInSearchResult");

        migrationBuilder.AddPrimaryKey(
            name: "PK_CardInSearchResult",
            table: "CardInSearchResult",
            columns: new[] { "SearchSubscriptionId", "CardId" });

        migrationBuilder.AddForeignKey(
            name: "FK_CardInSearchResult_SearchSubscriptions_SearchSubscriptionId",
            table: "CardInSearchResult",
            column: "SearchSubscriptionId",
            principalTable: "SearchSubscriptions",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }
}
