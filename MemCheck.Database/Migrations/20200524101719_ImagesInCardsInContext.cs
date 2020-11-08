using Microsoft.EntityFrameworkCore.Migrations;

namespace MemCheck.Database.Migrations
{
    public partial class ImagesInCardsInContext : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ImageInCard_Cards_CardId",
                table: "ImageInCard");

            migrationBuilder.DropForeignKey(
                name: "FK_ImageInCard_Images_ImageId1_ImageName",
                table: "ImageInCard");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ImageInCard",
                table: "ImageInCard");

            migrationBuilder.RenameTable(
                name: "ImageInCard",
                newName: "ImagesInCards");

            migrationBuilder.RenameIndex(
                name: "IX_ImageInCard_ImageId1_ImageName",
                table: "ImagesInCards",
                newName: "IX_ImagesInCards_ImageId1_ImageName");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ImagesInCards",
                table: "ImagesInCards",
                columns: new[] { "CardId", "ImageId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ImagesInCards_Cards_CardId",
                table: "ImagesInCards",
                column: "CardId",
                principalTable: "Cards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ImagesInCards_Images_ImageId1_ImageName",
                table: "ImagesInCards",
                columns: new[] { "ImageId1", "ImageName" },
                principalTable: "Images",
                principalColumns: new[] { "Id", "Name" },
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ImagesInCards_Cards_CardId",
                table: "ImagesInCards");

            migrationBuilder.DropForeignKey(
                name: "FK_ImagesInCards_Images_ImageId1_ImageName",
                table: "ImagesInCards");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ImagesInCards",
                table: "ImagesInCards");

            migrationBuilder.RenameTable(
                name: "ImagesInCards",
                newName: "ImageInCard");

            migrationBuilder.RenameIndex(
                name: "IX_ImagesInCards_ImageId1_ImageName",
                table: "ImageInCard",
                newName: "IX_ImageInCard_ImageId1_ImageName");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ImageInCard",
                table: "ImageInCard",
                columns: new[] { "CardId", "ImageId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ImageInCard_Cards_CardId",
                table: "ImageInCard",
                column: "CardId",
                principalTable: "Cards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ImageInCard_Images_ImageId1_ImageName",
                table: "ImageInCard",
                columns: new[] { "ImageId1", "ImageName" },
                principalTable: "Images",
                principalColumns: new[] { "Id", "Name" },
                onDelete: ReferentialAction.Cascade);
        }
    }
}
