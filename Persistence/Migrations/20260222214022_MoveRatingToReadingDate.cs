using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations;

/// <inheritdoc />
public partial class MoveRatingToReadingDate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Rating",
            table: "Books");

        migrationBuilder.AddColumn<int>(
            name: "Rating",
            table: "ReadingDates",
            type: "integer",
            nullable: false,
            defaultValue: 0);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Rating",
            table: "ReadingDates");

        migrationBuilder.AddColumn<int>(
            name: "Rating",
            table: "Books",
            type: "integer",
            nullable: false,
            defaultValue: 0);
    }
}
