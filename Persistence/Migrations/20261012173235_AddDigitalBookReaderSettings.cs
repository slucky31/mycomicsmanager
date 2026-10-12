using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations;

/// <inheritdoc />
public partial class AddDigitalBookReaderSettings : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "ReaderLayout",
            table: "DigitalBooks",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ReaderScrolling",
            table: "DigitalBooks",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ReadingDirection",
            table: "DigitalBooks",
            type: "integer",
            nullable: false,
            defaultValue: 0);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ReaderLayout",
            table: "DigitalBooks");

        migrationBuilder.DropColumn(
            name: "ReaderScrolling",
            table: "DigitalBooks");

        migrationBuilder.DropColumn(
            name: "ReadingDirection",
            table: "DigitalBooks");
    }
}
