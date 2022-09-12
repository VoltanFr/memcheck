using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemCheck.Database.Migrations;

public partial class ImageInCard : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ImagesInCards",
            columns: table => new
            {
                ImageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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
            name: "IX_ImagesInCards_CardId",
            table: "ImagesInCards",
            column: "CardId");

        migrationBuilder.CreateIndex(
            name: "IX_ImagesInCards_ImageId",
            table: "ImagesInCards",
            column: "ImageId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ImagesInCards");
    }
}
