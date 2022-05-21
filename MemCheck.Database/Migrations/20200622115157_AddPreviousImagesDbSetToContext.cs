using Microsoft.EntityFrameworkCore.Migrations;

namespace MemCheck.Database.Migrations;

public partial class AddPreviousImagesDbSetToContext : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_ImagePreviousVersion_Images_ImageId",
            table: "ImagePreviousVersion");

        migrationBuilder.DropForeignKey(
            name: "FK_ImagePreviousVersion_AspNetUsers_OwnerId",
            table: "ImagePreviousVersion");

        migrationBuilder.DropForeignKey(
            name: "FK_ImagePreviousVersion_ImagePreviousVersion_PreviousVersionId",
            table: "ImagePreviousVersion");

        migrationBuilder.DropPrimaryKey(
            name: "PK_ImagePreviousVersion",
            table: "ImagePreviousVersion");

        migrationBuilder.RenameTable(
            name: "ImagePreviousVersion",
            newName: "ImagePreviousVersions");

        migrationBuilder.RenameIndex(
            name: "IX_ImagePreviousVersion_PreviousVersionId",
            table: "ImagePreviousVersions",
            newName: "IX_ImagePreviousVersions_PreviousVersionId");

        migrationBuilder.RenameIndex(
            name: "IX_ImagePreviousVersion_OwnerId",
            table: "ImagePreviousVersions",
            newName: "IX_ImagePreviousVersions_OwnerId");

        migrationBuilder.RenameIndex(
            name: "IX_ImagePreviousVersion_ImageId",
            table: "ImagePreviousVersions",
            newName: "IX_ImagePreviousVersions_ImageId");

        migrationBuilder.AddPrimaryKey(
            name: "PK_ImagePreviousVersions",
            table: "ImagePreviousVersions",
            column: "Id");

        migrationBuilder.AddForeignKey(
            name: "FK_ImagePreviousVersions_Images_ImageId",
            table: "ImagePreviousVersions",
            column: "ImageId",
            principalTable: "Images",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_ImagePreviousVersions_AspNetUsers_OwnerId",
            table: "ImagePreviousVersions",
            column: "OwnerId",
            principalTable: "AspNetUsers",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_ImagePreviousVersions_ImagePreviousVersions_PreviousVersionId",
            table: "ImagePreviousVersions",
            column: "PreviousVersionId",
            principalTable: "ImagePreviousVersions",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_ImagePreviousVersions_Images_ImageId",
            table: "ImagePreviousVersions");

        migrationBuilder.DropForeignKey(
            name: "FK_ImagePreviousVersions_AspNetUsers_OwnerId",
            table: "ImagePreviousVersions");

        migrationBuilder.DropForeignKey(
            name: "FK_ImagePreviousVersions_ImagePreviousVersions_PreviousVersionId",
            table: "ImagePreviousVersions");

        migrationBuilder.DropPrimaryKey(
            name: "PK_ImagePreviousVersions",
            table: "ImagePreviousVersions");

        migrationBuilder.RenameTable(
            name: "ImagePreviousVersions",
            newName: "ImagePreviousVersion");

        migrationBuilder.RenameIndex(
            name: "IX_ImagePreviousVersions_PreviousVersionId",
            table: "ImagePreviousVersion",
            newName: "IX_ImagePreviousVersion_PreviousVersionId");

        migrationBuilder.RenameIndex(
            name: "IX_ImagePreviousVersions_OwnerId",
            table: "ImagePreviousVersion",
            newName: "IX_ImagePreviousVersion_OwnerId");

        migrationBuilder.RenameIndex(
            name: "IX_ImagePreviousVersions_ImageId",
            table: "ImagePreviousVersion",
            newName: "IX_ImagePreviousVersion_ImageId");

        migrationBuilder.AddPrimaryKey(
            name: "PK_ImagePreviousVersion",
            table: "ImagePreviousVersion",
            column: "Id");

        migrationBuilder.AddForeignKey(
            name: "FK_ImagePreviousVersion_Images_ImageId",
            table: "ImagePreviousVersion",
            column: "ImageId",
            principalTable: "Images",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_ImagePreviousVersion_AspNetUsers_OwnerId",
            table: "ImagePreviousVersion",
            column: "OwnerId",
            principalTable: "AspNetUsers",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_ImagePreviousVersion_ImagePreviousVersion_PreviousVersionId",
            table: "ImagePreviousVersion",
            column: "PreviousVersionId",
            principalTable: "ImagePreviousVersion",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }
}
