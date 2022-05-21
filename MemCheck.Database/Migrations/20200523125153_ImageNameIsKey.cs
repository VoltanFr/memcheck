using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace MemCheck.Database.Migrations;

public partial class ImageNameIsKey : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_ImageInCard_Images_ImageId",
            table: "ImageInCard");

        migrationBuilder.DropPrimaryKey(
            name: "PK_Images",
            table: "Images");

        migrationBuilder.DropIndex(
            name: "IX_ImageInCard_ImageId",
            table: "ImageInCard");

        migrationBuilder.AlterColumn<string>(
            name: "Name",
            table: "Images",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(max)");

        migrationBuilder.AddColumn<Guid>(
            name: "ImageId1",
            table: "ImageInCard",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

        migrationBuilder.AddColumn<string>(
            name: "ImageName",
            table: "ImageInCard",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddPrimaryKey(
            name: "PK_Images",
            table: "Images",
            columns: new[] { "Id", "Name" });

        migrationBuilder.CreateIndex(
            name: "IX_ImageInCard_ImageId1_ImageName",
            table: "ImageInCard",
            columns: new[] { "ImageId1", "ImageName" });

        migrationBuilder.AddForeignKey(
            name: "FK_ImageInCard_Images_ImageId1_ImageName",
            table: "ImageInCard",
            columns: new[] { "ImageId1", "ImageName" },
            principalTable: "Images",
            principalColumns: new[] { "Id", "Name" },
            onDelete: ReferentialAction.Cascade);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_ImageInCard_Images_ImageId1_ImageName",
            table: "ImageInCard");

        migrationBuilder.DropPrimaryKey(
            name: "PK_Images",
            table: "Images");

        migrationBuilder.DropIndex(
            name: "IX_ImageInCard_ImageId1_ImageName",
            table: "ImageInCard");

        migrationBuilder.DropColumn(
            name: "ImageId1",
            table: "ImageInCard");

        migrationBuilder.DropColumn(
            name: "ImageName",
            table: "ImageInCard");

        migrationBuilder.AlterColumn<string>(
            name: "Name",
            table: "Images",
            type: "nvarchar(max)",
            nullable: false,
            oldClrType: typeof(string));

        migrationBuilder.AddPrimaryKey(
            name: "PK_Images",
            table: "Images",
            column: "Id");

        migrationBuilder.CreateIndex(
            name: "IX_ImageInCard_ImageId",
            table: "ImageInCard",
            column: "ImageId");

        migrationBuilder.AddForeignKey(
            name: "FK_ImageInCard_Images_ImageId",
            table: "ImageInCard",
            column: "ImageId",
            principalTable: "Images",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }
}
