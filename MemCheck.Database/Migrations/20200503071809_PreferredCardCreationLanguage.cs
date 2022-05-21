using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace MemCheck.Database.Migrations;

public partial class PreferredCardCreationLanguage : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "PreferredCardCreationLanguageId",
            table: "AspNetUsers",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_AspNetUsers_PreferredCardCreationLanguageId",
            table: "AspNetUsers",
            column: "PreferredCardCreationLanguageId");

        migrationBuilder.AddForeignKey(
            name: "FK_AspNetUsers_CardLanguages_PreferredCardCreationLanguageId",
            table: "AspNetUsers",
            column: "PreferredCardCreationLanguageId",
            principalTable: "CardLanguages",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_AspNetUsers_CardLanguages_PreferredCardCreationLanguageId",
            table: "AspNetUsers");

        migrationBuilder.DropIndex(
            name: "IX_AspNetUsers_PreferredCardCreationLanguageId",
            table: "AspNetUsers");

        migrationBuilder.DropColumn(
            name: "PreferredCardCreationLanguageId",
            table: "AspNetUsers");
    }
}
