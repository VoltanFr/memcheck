using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemCheck.Database.Migrations
{
    public partial class DemoDownloadAuditTrailEntry : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DemoDownloadAuditTrailEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TagId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DownloadUtcDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CountOfCardsReturned = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemoDownloadAuditTrailEntries", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DemoDownloadAuditTrailEntries");
        }
    }
}
