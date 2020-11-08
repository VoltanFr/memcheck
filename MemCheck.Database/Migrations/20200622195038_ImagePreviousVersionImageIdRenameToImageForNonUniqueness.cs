using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MemCheck.Database.Migrations
{
    public partial class ImagePreviousVersionImageIdRenameToImageForNonUniqueness : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ImagePreviousVersions_Images_ImageId",
                table: "ImagePreviousVersions");

            migrationBuilder.DropIndex(
                name: "IX_ImagePreviousVersions_ImageId",
                table: "ImagePreviousVersions");

            migrationBuilder.RenameColumn(
                name: "ImageId",
                newName: "Image",
                table: "ImagePreviousVersions");

            migrationBuilder.AddColumn<Guid>(
                name: "PreviousVersionId",
                table: "Images",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Images_PreviousVersionId",
                table: "Images",
                column: "PreviousVersionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Images_ImagePreviousVersions_PreviousVersionId",
                table: "Images",
                column: "PreviousVersionId",
                principalTable: "ImagePreviousVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Images_ImagePreviousVersions_PreviousVersionId",
                table: "Images");

            migrationBuilder.DropIndex(
                name: "IX_Images_PreviousVersionId",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "PreviousVersionId",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "Image",
                table: "ImagePreviousVersions");

            migrationBuilder.AddColumn<Guid>(
                name: "ImageId",
                table: "ImagePreviousVersions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_ImagePreviousVersions_ImageId",
                table: "ImagePreviousVersions",
                column: "ImageId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ImagePreviousVersions_Images_ImageId",
                table: "ImagePreviousVersions",
                column: "ImageId",
                principalTable: "Images",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
