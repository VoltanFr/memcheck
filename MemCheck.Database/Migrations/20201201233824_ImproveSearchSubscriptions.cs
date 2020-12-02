using Microsoft.EntityFrameworkCore.Migrations;

namespace MemCheck.Database.Migrations
{
    public partial class ImproveSearchSubscriptions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExcludedTagInSearchSubscription_SearchSubscriptions_SearchId",
                table: "ExcludedTagInSearchSubscription");

            migrationBuilder.DropForeignKey(
                name: "FK_RequiredTagInSearchSubscription_SearchSubscriptions_SearchId",
                table: "RequiredTagInSearchSubscription");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RequiredTagInSearchSubscription",
                table: "RequiredTagInSearchSubscription");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ExcludedTagInSearchSubscription",
                table: "ExcludedTagInSearchSubscription");

            migrationBuilder.RenameTable(
                name: "RequiredTagInSearchSubscription",
                newName: "RequiredTagInSearchSubscriptions");

            migrationBuilder.RenameTable(
                name: "ExcludedTagInSearchSubscription",
                newName: "ExcludedTagInSearchSubscriptions");

            migrationBuilder.RenameColumn(
                name: "SearchId",
                table: "RequiredTagInSearchSubscriptions",
                newName: "SearchSubscriptionId");

            migrationBuilder.RenameColumn(
                name: "SearchId",
                table: "ExcludedTagInSearchSubscriptions",
                newName: "SearchSubscriptionId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RequiredTagInSearchSubscriptions",
                table: "RequiredTagInSearchSubscriptions",
                columns: new[] { "SearchSubscriptionId", "TagId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_ExcludedTagInSearchSubscriptions",
                table: "ExcludedTagInSearchSubscriptions",
                columns: new[] { "SearchSubscriptionId", "TagId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ExcludedTagInSearchSubscriptions_SearchSubscriptions_SearchSubscriptionId",
                table: "ExcludedTagInSearchSubscriptions",
                column: "SearchSubscriptionId",
                principalTable: "SearchSubscriptions",
                principalColumn: "SearchId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RequiredTagInSearchSubscriptions_SearchSubscriptions_SearchSubscriptionId",
                table: "RequiredTagInSearchSubscriptions",
                column: "SearchSubscriptionId",
                principalTable: "SearchSubscriptions",
                principalColumn: "SearchId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExcludedTagInSearchSubscriptions_SearchSubscriptions_SearchSubscriptionId",
                table: "ExcludedTagInSearchSubscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_RequiredTagInSearchSubscriptions_SearchSubscriptions_SearchSubscriptionId",
                table: "RequiredTagInSearchSubscriptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RequiredTagInSearchSubscriptions",
                table: "RequiredTagInSearchSubscriptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ExcludedTagInSearchSubscriptions",
                table: "ExcludedTagInSearchSubscriptions");

            migrationBuilder.RenameTable(
                name: "RequiredTagInSearchSubscriptions",
                newName: "RequiredTagInSearchSubscription");

            migrationBuilder.RenameTable(
                name: "ExcludedTagInSearchSubscriptions",
                newName: "ExcludedTagInSearchSubscription");

            migrationBuilder.RenameColumn(
                name: "SearchSubscriptionId",
                table: "RequiredTagInSearchSubscription",
                newName: "SearchId");

            migrationBuilder.RenameColumn(
                name: "SearchSubscriptionId",
                table: "ExcludedTagInSearchSubscription",
                newName: "SearchId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RequiredTagInSearchSubscription",
                table: "RequiredTagInSearchSubscription",
                columns: new[] { "SearchId", "TagId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_ExcludedTagInSearchSubscription",
                table: "ExcludedTagInSearchSubscription",
                columns: new[] { "SearchId", "TagId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ExcludedTagInSearchSubscription_SearchSubscriptions_SearchId",
                table: "ExcludedTagInSearchSubscription",
                column: "SearchId",
                principalTable: "SearchSubscriptions",
                principalColumn: "SearchId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RequiredTagInSearchSubscription_SearchSubscriptions_SearchId",
                table: "RequiredTagInSearchSubscription",
                column: "SearchId",
                principalTable: "SearchSubscriptions",
                principalColumn: "SearchId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
