﻿// <auto-generated/>

using Microsoft.EntityFrameworkCore.Migrations;

namespace MemCheck.Database.Migrations
{
    public partial class ImageRedesigned : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "Size",
                table: "Images");

            migrationBuilder.AlterColumn<byte[]>(
                name: "MediumBlob",
                table: "Images",
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "BigBlob",
                table: "Images",
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "OriginalBlob",
                table: "Images",
                nullable: false,
                defaultValue: new byte[] { });

            migrationBuilder.AddColumn<string>(
                name: "OriginalContentType",
                table: "Images",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "OriginalSize",
                table: "Images",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OriginalBlob",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "OriginalContentType",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "OriginalSize",
                table: "Images");

            migrationBuilder.AlterColumn<byte[]>(
                name: "MediumBlob",
                table: "Images",
                type: "varbinary(max)",
                nullable: true,
                oldClrType: typeof(byte[]));

            migrationBuilder.AlterColumn<byte[]>(
                name: "BigBlob",
                table: "Images",
                type: "varbinary(max)",
                nullable: true,
                oldClrType: typeof(byte[]));

            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "Images",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Size",
                table: "Images",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
