using Microsoft.EntityFrameworkCore.Migrations;

namespace MemCheck.Database.Migrations;

public partial class AddRatingsInCard : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<double>(
            name: "AverageRating",
            table: "Cards",
            type: "float",
            nullable: false,
            defaultValue: 0.0);

        migrationBuilder.AddColumn<int>(
            name: "RatingCount",
            table: "Cards",
            type: "int",
            nullable: false,
            defaultValue: 0);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "AverageRating",
            table: "Cards");

        migrationBuilder.DropColumn(
            name: "RatingCount",
            table: "Cards");
    }
}
