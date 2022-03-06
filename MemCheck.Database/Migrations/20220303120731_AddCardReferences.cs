using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemCheck.Database.Migrations
{
    public partial class AddCardReferences : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "References",
                table: "Cards",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "References",
                table: "Cards");
        }
    }
}
