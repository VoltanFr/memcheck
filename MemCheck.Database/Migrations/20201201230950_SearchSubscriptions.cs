using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace MemCheck.Database.Migrations;

public partial class SearchSubscriptions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "SearchSubscriptions",
            columns: table => new
            {
                SearchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ExcludedDeck = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RequiredText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                RegistrationUtcDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                LastNotificationUtcDate = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SearchSubscriptions", x => x.SearchId);
            });

        migrationBuilder.CreateTable(
            name: "ExcludedTagInSearchSubscription",
            columns: table => new
            {
                SearchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TagId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ExcludedTagInSearchSubscription", x => new { x.SearchId, x.TagId });
                table.ForeignKey(
                    name: "FK_ExcludedTagInSearchSubscription_SearchSubscriptions_SearchId",
                    column: x => x.SearchId,
                    principalTable: "SearchSubscriptions",
                    principalColumn: "SearchId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "RequiredTagInSearchSubscription",
            columns: table => new
            {
                SearchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TagId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RequiredTagInSearchSubscription", x => new { x.SearchId, x.TagId });
                table.ForeignKey(
                    name: "FK_RequiredTagInSearchSubscription_SearchSubscriptions_SearchId",
                    column: x => x.SearchId,
                    principalTable: "SearchSubscriptions",
                    principalColumn: "SearchId",
                    onDelete: ReferentialAction.Cascade);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ExcludedTagInSearchSubscription");

        migrationBuilder.DropTable(
            name: "RequiredTagInSearchSubscription");

        migrationBuilder.DropTable(
            name: "SearchSubscriptions");
    }
}
