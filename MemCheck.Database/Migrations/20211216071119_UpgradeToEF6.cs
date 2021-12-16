using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemCheck.Database.Migrations
{
    public partial class UpgradeToEF6 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //The two lines below were added by me, to support index renaming in EF6
            migrationBuilder.CreateIndex("IX_Cards_VersionCreatorId", "Cards", "VersionCreatorId");
            migrationBuilder.DropIndex("IX_Cards_OwnerId", "Cards");

            migrationBuilder.DropForeignKey(
                name: "FK_CardPreviousVersions_AspNetUsers_VersionCreatorId",
                table: "CardPreviousVersions");

            migrationBuilder.DropForeignKey(
                name: "FK_CardPreviousVersions_CardLanguages_CardLanguageId",
                table: "CardPreviousVersions");

            migrationBuilder.DropForeignKey(
                name: "FK_Cards_AspNetUsers_VersionCreatorId",
                table: "Cards");

            migrationBuilder.DropForeignKey(
                name: "FK_Cards_CardLanguages_CardLanguageId",
                table: "Cards");

            migrationBuilder.DropForeignKey(
                name: "FK_Decks_AspNetUsers_OwnerId",
                table: "Decks");

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
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "OwnerId",
                table: "ImagePreviousVersions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "OwnerId",
                table: "Decks",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "VersionCreatorId",
                table: "Cards",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CardLanguageId",
                table: "Cards",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "VersionCreatorId",
                table: "CardPreviousVersions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CardLanguageId",
                table: "CardPreviousVersions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            //migrationBuilder.CreateIndex("IX_Cards_VersionCreatorId", "Cards", "VersionCreatorId");

            migrationBuilder.AddForeignKey(
                name: "FK_CardPreviousVersions_AspNetUsers_VersionCreatorId",
                table: "CardPreviousVersions",
                column: "VersionCreatorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CardPreviousVersions_CardLanguages_CardLanguageId",
                table: "CardPreviousVersions",
                column: "CardLanguageId",
                principalTable: "CardLanguages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Cards_AspNetUsers_VersionCreatorId",
                table: "Cards",
                column: "VersionCreatorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Cards_CardLanguages_CardLanguageId",
                table: "Cards",
                column: "CardLanguageId",
                principalTable: "CardLanguages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Decks_AspNetUsers_OwnerId",
                table: "Decks",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ImagePreviousVersions_AspNetUsers_OwnerId",
                table: "ImagePreviousVersions",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Images_AspNetUsers_OwnerId",
                table: "Images",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CardPreviousVersions_AspNetUsers_VersionCreatorId",
                table: "CardPreviousVersions");

            migrationBuilder.DropForeignKey(
                name: "FK_CardPreviousVersions_CardLanguages_CardLanguageId",
                table: "CardPreviousVersions");

            migrationBuilder.DropForeignKey(
                name: "FK_Cards_AspNetUsers_VersionCreatorId",
                table: "Cards");

            migrationBuilder.DropForeignKey(
                name: "FK_Cards_CardLanguages_CardLanguageId",
                table: "Cards");

            migrationBuilder.DropForeignKey(
                name: "FK_Decks_AspNetUsers_OwnerId",
                table: "Decks");

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
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "OwnerId",
                table: "ImagePreviousVersions",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "OwnerId",
                table: "Decks",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "VersionCreatorId",
                table: "Cards",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "CardLanguageId",
                table: "Cards",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "VersionCreatorId",
                table: "CardPreviousVersions",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "CardLanguageId",
                table: "CardPreviousVersions",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddForeignKey(
                name: "FK_CardPreviousVersions_AspNetUsers_VersionCreatorId",
                table: "CardPreviousVersions",
                column: "VersionCreatorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CardPreviousVersions_CardLanguages_CardLanguageId",
                table: "CardPreviousVersions",
                column: "CardLanguageId",
                principalTable: "CardLanguages",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Cards_AspNetUsers_VersionCreatorId",
                table: "Cards",
                column: "VersionCreatorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Cards_CardLanguages_CardLanguageId",
                table: "Cards",
                column: "CardLanguageId",
                principalTable: "CardLanguages",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Decks_AspNetUsers_OwnerId",
                table: "Decks",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ImagePreviousVersions_AspNetUsers_OwnerId",
                table: "ImagePreviousVersions",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Images_AspNetUsers_OwnerId",
                table: "Images",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
