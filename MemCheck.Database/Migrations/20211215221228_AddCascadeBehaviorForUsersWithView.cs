using Microsoft.EntityFrameworkCore.Migrations;

namespace MemCheck.Database.Migrations
{
    public partial class AddCascadeBehaviorForUsersWithView : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UsersWithViewOnCards_AspNetUsers_UserId",
                table: "UsersWithViewOnCards");

            migrationBuilder.AddForeignKey(
                name: "FK_UsersWithViewOnCards_AspNetUsers_UserId",
                table: "UsersWithViewOnCards",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UsersWithViewOnCards_AspNetUsers_UserId",
                table: "UsersWithViewOnCards");

            migrationBuilder.AddForeignKey(
                name: "FK_UsersWithViewOnCards_AspNetUsers_UserId",
                table: "UsersWithViewOnCards",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
