using Microsoft.EntityFrameworkCore.Migrations;

namespace MemCheck.Database.Migrations;

public partial class RemoveHomePageReloadInterval : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "HomePageReloadInterval",
            table: "AspNetUsers");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "HomePageReloadInterval",
            table: "AspNetUsers",
            type: "int",
            nullable: false,
            defaultValue: 0);
    }
}
