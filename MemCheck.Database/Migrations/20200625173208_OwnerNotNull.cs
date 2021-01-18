using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace MemCheck.Database.Migrations
{
    public partial class OwnerNotNull : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropForeignKey(
            //    name: "FK_Cards_AspNetUsers_OwnerId",
            //    table: "Cards");

            //migrationBuilder.DropForeignKey(
            //    name: "FK_ImagePreviousVersions_AspNetUsers_OwnerId",
            //    table: "ImagePreviousVersions");

            //migrationBuilder.DropForeignKey(
            //    name: "FK_Images_AspNetUsers_OwnerId",
            //    table: "Images");

            migrationBuilder.AlterColumn<Guid>(
                name: "OwnerId",
                table: "Images",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "OwnerId",
                table: "ImagePreviousVersions",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "OwnerId",
                table: "Cards",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            //migrationBuilder.AddForeignKey(
            //    name: "FK_Cards_AspNetUsers_OwnerId",
            //    table: "Cards",
            //    column: "OwnerId",
            //    principalTable: "AspNetUsers",
            //    principalColumn: "Id",
            //    onDelete: ReferentialAction.Cascade);

            //migrationBuilder.AddForeignKey(
            //    name: "FK_ImagePreviousVersions_AspNetUsers_OwnerId",
            //    table: "ImagePreviousVersions",
            //    column: "OwnerId",
            //    principalTable: "AspNetUsers",
            //    principalColumn: "Id",
            //    onDelete: ReferentialAction.Cascade);

            //migrationBuilder.AddForeignKey(
            //    name: "FK_Images_AspNetUsers_OwnerId",
            //    table: "Images",
            //    column: "OwnerId",
            //    principalTable: "AspNetUsers",
            //    principalColumn: "Id",
            //    onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cards_AspNetUsers_OwnerId",
                table: "Cards");

            migrationBuilder.DropForeignKey(
                name: "FK_ImagePreviousVersions_AspNetUsers_OwnerId",
                table: "ImagePreviousVersions");

            migrationBuilder.DropForeignKey(
                name: "FK_Images_AspNetUsers_OwnerId",
                table: "Images");

            migrationBuilder.AlterColumn<Guid>(
                name: "OwnerId",
                table: "Images",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid));

            migrationBuilder.AlterColumn<Guid>(
                name: "OwnerId",
                table: "ImagePreviousVersions",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid));

            migrationBuilder.AlterColumn<Guid>(
                name: "OwnerId",
                table: "Cards",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid));

            migrationBuilder.AddForeignKey(
                name: "FK_Cards_AspNetUsers_OwnerId",
                table: "Cards",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ImagePreviousVersions_AspNetUsers_OwnerId",
                table: "ImagePreviousVersions",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Images_AspNetUsers_OwnerId",
                table: "Images",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
