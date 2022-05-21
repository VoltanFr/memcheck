using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace MemCheck.Database.Migrations;

public partial class AddImagesToCards : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ImageInCard",
            columns: table => new
            {
                ImageId = table.Column<Guid>(nullable: false),
                CardId = table.Column<Guid>(nullable: false),
                CardSide = table.Column<int>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ImageInCard", x => new { x.CardId, x.ImageId });
                table.ForeignKey(
                    name: "FK_ImageInCard_Cards_CardId",
                    column: x => x.CardId,
                    principalTable: "Cards",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ImageInCard_Images_ImageId",
                    column: x => x.ImageId,
                    principalTable: "Images",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ImageInCard_ImageId",
            table: "ImageInCard",
            column: "ImageId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ImageInCard");
    }
}
