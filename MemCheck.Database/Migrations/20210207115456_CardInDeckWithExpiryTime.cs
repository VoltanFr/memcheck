using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MemCheck.Database.Migrations
{
    public partial class CardInDeckWithExpiryTime : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryUtcTime",
                table: "CardsInDecks",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpiryUtcTime",
                table: "CardsInDecks");
        }
    }
}
