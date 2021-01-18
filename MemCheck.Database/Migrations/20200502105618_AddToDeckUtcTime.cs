using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace MemCheck.Database.Migrations
{
    public partial class AddToDeckUtcTime : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AddToDeckUtcTime",
                table: "CardsInDecks",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AddToDeckUtcTime",
                table: "CardsInDecks");
        }
    }
}
