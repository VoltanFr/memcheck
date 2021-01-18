using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace MemCheck.Database.Migrations
{
    public partial class ImageVersioning : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "InitialUploadUtcDate",
                table: "Images",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastChangeUtcDate",
                table: "Images",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "VersionDescription",
                table: "Images",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InitialUploadUtcDate",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "LastChangeUtcDate",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "VersionDescription",
                table: "Images");
        }
    }
}
