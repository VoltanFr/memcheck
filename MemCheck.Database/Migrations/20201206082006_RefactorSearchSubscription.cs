using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace MemCheck.Database.Migrations;

public partial class RefactorSearchSubscription : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "LastNotificationUtcDate",
            table: "SearchSubscriptions",
            newName: "LastRunUtcDate");

        migrationBuilder.RenameColumn(
            name: "SearchId",
            table: "SearchSubscriptions",
            newName: "Id");

        migrationBuilder.CreateTable(
            name: "CardInSearchResult",
            columns: table => new
            {
                SearchSubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CardInSearchResult", x => new { x.SearchSubscriptionId, x.CardId });
                table.ForeignKey(
                    name: "FK_CardInSearchResult_SearchSubscriptions_SearchSubscriptionId",
                    column: x => x.SearchSubscriptionId,
                    principalTable: "SearchSubscriptions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "CardInSearchResult");

        migrationBuilder.RenameColumn(
            name: "LastRunUtcDate",
            table: "SearchSubscriptions",
            newName: "LastNotificationUtcDate");

        migrationBuilder.RenameColumn(
            name: "Id",
            table: "SearchSubscriptions",
            newName: "SearchId");
    }
}
