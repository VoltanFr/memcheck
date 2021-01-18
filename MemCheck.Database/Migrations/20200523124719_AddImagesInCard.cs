﻿using Microsoft.EntityFrameworkCore.Migrations;

namespace MemCheck.Database.Migrations
{
    public partial class AddImagesInCard : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Blob",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "Format",
                table: "Images");

            migrationBuilder.AddColumn<byte[]>(
                name: "BigBlob",
                table: "Images",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "MediumBlob",
                table: "Images",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "SmallBlob",
                table: "Images",
                nullable: false,
                defaultValue: new byte[] { });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BigBlob",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "MediumBlob",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "SmallBlob",
                table: "Images");

            migrationBuilder.AddColumn<byte[]>(
                name: "Blob",
                table: "Images",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[] { });

            migrationBuilder.AddColumn<int>(
                name: "Format",
                table: "Images",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
