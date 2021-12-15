using Microsoft.EntityFrameworkCore.Migrations;

namespace MemCheck.Database.Migrations
{
    public partial class AddCascadeBehaviorForImageInCardPreviousVersion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ImagesInCardPreviousVersions_Images_ImageId",
                table: "ImagesInCardPreviousVersions");

            migrationBuilder.AddForeignKey(
                name: "FK_ImagesInCardPreviousVersions_Images_ImageId",
                table: "ImagesInCardPreviousVersions",
                column: "ImageId",
                principalTable: "Images",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ImagesInCardPreviousVersions_Images_ImageId",
                table: "ImagesInCardPreviousVersions");

            migrationBuilder.AddForeignKey(
                name: "FK_ImagesInCardPreviousVersions_Images_ImageId",
                table: "ImagesInCardPreviousVersions",
                column: "ImageId",
                principalTable: "Images",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
