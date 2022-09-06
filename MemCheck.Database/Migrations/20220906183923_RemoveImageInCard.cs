using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemCheck.Database.Migrations;

public partial class RemoveImageInCard : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ImagesInCardPreviousVersions");

        migrationBuilder.DropTable(
            name: "ImagesInCards");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ImagesInCardPreviousVersions",
            columns: table => new
            {
                ImageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CardPreviousVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CardSide = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ImagesInCardPreviousVersions", x => new { x.ImageId, x.CardPreviousVersionId });
                table.ForeignKey(
                    name: "FK_ImagesInCardPreviousVersions_CardPreviousVersions_CardPreviousVersionId",
                    column: x => x.CardPreviousVersionId,
                    principalTable: "CardPreviousVersions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ImagesInCardPreviousVersions_Images_ImageId",
                    column: x => x.ImageId,
                    principalTable: "Images",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateTable(
            name: "ImagesInCards",
            columns: table => new
            {
                CardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ImageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CardSide = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ImagesInCards", x => new { x.CardId, x.ImageId });
                table.ForeignKey(
                    name: "FK_ImagesInCards_Cards_CardId",
                    column: x => x.CardId,
                    principalTable: "Cards",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ImagesInCards_Images_ImageId",
                    column: x => x.ImageId,
                    principalTable: "Images",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateIndex(
            name: "IX_ImagesInCardPreviousVersions_CardPreviousVersionId",
            table: "ImagesInCardPreviousVersions",
            column: "CardPreviousVersionId");

        migrationBuilder.CreateIndex(
            name: "IX_ImagesInCards_ImageId",
            table: "ImagesInCards",
            column: "ImageId");
    }
}
