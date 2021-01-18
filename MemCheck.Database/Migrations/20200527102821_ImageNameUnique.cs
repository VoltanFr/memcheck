using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace MemCheck.Database.Migrations
{
    public partial class ImageNameUnique : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ImagesInCards_Images_ImageId1_ImageName",
                table: "ImagesInCards");

            migrationBuilder.DropIndex(
                name: "IX_ImagesInCards_ImageId1_ImageName",
                table: "ImagesInCards");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Images",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "ImageId1",
                table: "ImagesInCards");

            migrationBuilder.DropColumn(
                name: "ImageName",
                table: "ImagesInCards");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Images",
                table: "Images",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_ImagesInCards_ImageId",
                table: "ImagesInCards",
                column: "ImageId");

            migrationBuilder.CreateIndex(
                name: "IX_Images_Name",
                table: "Images",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ImagesInCards_Images_ImageId",
                table: "ImagesInCards",
                column: "ImageId",
                principalTable: "Images",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ImagesInCards_Images_ImageId",
                table: "ImagesInCards");

            migrationBuilder.DropIndex(
                name: "IX_ImagesInCards_ImageId",
                table: "ImagesInCards");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Images",
                table: "Images");

            migrationBuilder.DropIndex(
                name: "IX_Images_Name",
                table: "Images");

            migrationBuilder.AddColumn<Guid>(
                name: "ImageId1",
                table: "ImagesInCards",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "ImageName",
                table: "ImagesInCards",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Images",
                table: "Images",
                columns: new[] { "Id", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_ImagesInCards_ImageId1_ImageName",
                table: "ImagesInCards",
                columns: new[] { "ImageId1", "ImageName" });

            migrationBuilder.AddForeignKey(
                name: "FK_ImagesInCards_Images_ImageId1_ImageName",
                table: "ImagesInCards",
                columns: new[] { "ImageId1", "ImageName" },
                principalTable: "Images",
                principalColumns: new[] { "Id", "Name" },
                onDelete: ReferentialAction.Cascade);
        }
    }
}
