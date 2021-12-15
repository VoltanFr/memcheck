using Microsoft.EntityFrameworkCore.Migrations;

namespace MemCheck.Database.Migrations
{
    public partial class UpgradeToDotNet6 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_UsersWithViewOnCardPreviousVersions_AllowedUserId",
                table: "UsersWithViewOnCardPreviousVersions",
                column: "AllowedUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_UsersWithViewOnCardPreviousVersions_AspNetUsers_AllowedUserId",
                table: "UsersWithViewOnCardPreviousVersions",
                column: "AllowedUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UsersWithViewOnCardPreviousVersions_AspNetUsers_AllowedUserId",
                table: "UsersWithViewOnCardPreviousVersions");

            migrationBuilder.DropIndex(
                name: "IX_UsersWithViewOnCardPreviousVersions_AllowedUserId",
                table: "UsersWithViewOnCardPreviousVersions");
        }
    }
}
