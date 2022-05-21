using Microsoft.EntityFrameworkCore.Migrations;

namespace MemCheck.Database.Migrations;

public partial class AddCascadeBehaviorForUserWithViewOnCardPreviousVersion : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_UsersWithViewOnCardPreviousVersions_AspNetUsers_AllowedUserId",
            table: "UsersWithViewOnCardPreviousVersions");

        migrationBuilder.AddForeignKey(
            name: "FK_UsersWithViewOnCardPreviousVersions_AspNetUsers_AllowedUserId",
            table: "UsersWithViewOnCardPreviousVersions",
            column: "AllowedUserId",
            principalTable: "AspNetUsers",
            principalColumn: "Id");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_UsersWithViewOnCardPreviousVersions_AspNetUsers_AllowedUserId",
            table: "UsersWithViewOnCardPreviousVersions");

        migrationBuilder.AddForeignKey(
            name: "FK_UsersWithViewOnCardPreviousVersions_AspNetUsers_AllowedUserId",
            table: "UsersWithViewOnCardPreviousVersions",
            column: "AllowedUserId",
            principalTable: "AspNetUsers",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }
}
