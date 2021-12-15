using Microsoft.EntityFrameworkCore.Migrations;

namespace MemCheck.Database.Migrations
{
    public partial class AddCascadeBehaviorForUserCardRating : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserCardRatings_AspNetUsers_UserId",
                table: "UserCardRatings");

            migrationBuilder.DropForeignKey(
                name: "FK_UserCardRatings_Cards_CardId",
                table: "UserCardRatings");

            migrationBuilder.AddForeignKey(
                name: "FK_UserCardRatings_AspNetUsers_UserId",
                table: "UserCardRatings",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserCardRatings_Cards_CardId",
                table: "UserCardRatings",
                column: "CardId",
                principalTable: "Cards",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserCardRatings_AspNetUsers_UserId",
                table: "UserCardRatings");

            migrationBuilder.DropForeignKey(
                name: "FK_UserCardRatings_Cards_CardId",
                table: "UserCardRatings");

            migrationBuilder.AddForeignKey(
                name: "FK_UserCardRatings_AspNetUsers_UserId",
                table: "UserCardRatings",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserCardRatings_Cards_CardId",
                table: "UserCardRatings",
                column: "CardId",
                principalTable: "Cards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
