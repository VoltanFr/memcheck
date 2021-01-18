using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace MemCheck.Database.Migrations
{
    public partial class AddNotifs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CardNotifications",
                columns: table => new
                {
                    CardId = table.Column<Guid>(nullable: false),
                    UserId = table.Column<Guid>(nullable: false),
                    RegistrationUtcDate = table.Column<DateTime>(nullable: false),
                    RegistrationMethod = table.Column<int>(nullable: false),
                    LastNotificationUtcDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardNotifications", x => new { x.CardId, x.UserId });
                    table.ForeignKey(
                        name: "FK_CardNotifications_Cards_CardId",
                        column: x => x.CardId,
                        principalTable: "Cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CardNotifications_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CardNotifications_UserId",
                table: "CardNotifications",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CardNotifications");
        }
    }
}
