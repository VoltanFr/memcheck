using Microsoft.EntityFrameworkCore.Migrations;

namespace MemCheck.Database.Migrations
{
    public partial class AddCascadeBehaviorForImageInCard : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ImagesInCards_Images_ImageId",
                table: "ImagesInCards");

            migrationBuilder.AddForeignKey(
                name: "FK_ImagesInCards_Images_ImageId",
                table: "ImagesInCards",
                column: "ImageId",
                principalTable: "Images",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ImagesInCards_Images_ImageId",
                table: "ImagesInCards");

            migrationBuilder.AddForeignKey(
                name: "FK_ImagesInCards_Images_ImageId",
                table: "ImagesInCards",
                column: "ImageId",
                principalTable: "Images",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
