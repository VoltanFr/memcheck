using Microsoft.EntityFrameworkCore.Migrations;

namespace MemCheck.Database.Migrations;

public partial class RemoveNavigationalPropertyFromNotif : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_CardNotifications_Cards_CardId",
            table: "CardNotifications");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddForeignKey(
            name: "FK_CardNotifications_Cards_CardId",
            table: "CardNotifications",
            column: "CardId",
            principalTable: "Cards",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }
}
