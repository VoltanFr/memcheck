using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace MemCheck.Database.Migrations;

public partial class AddUserAccountNotif : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "LastNotificationUtcDate",
            table: "AspNetUsers",
            type: "datetime2",
            nullable: false,
            defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

        migrationBuilder.AddColumn<int>(
            name: "MinimumCountOfDaysBetweenNotifs",
            table: "AspNetUsers",
            type: "int",
            nullable: false,
            defaultValue: 0);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "LastNotificationUtcDate",
            table: "AspNetUsers");

        migrationBuilder.DropColumn(
            name: "MinimumCountOfDaysBetweenNotifs",
            table: "AspNetUsers");
    }
}
