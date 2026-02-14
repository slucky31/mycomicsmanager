using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations;

/// <inheritdoc />
public partial class AddBookMetadata : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Authors",
            table: "Books",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<int>(
            name: "NumberOfPages",
            table: "Books",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<DateOnly>(
            name: "PublishDate",
            table: "Books",
            type: "date",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Publishers",
            table: "Books",
            type: "text",
            nullable: false,
            defaultValue: "");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Authors",
            table: "Books");

        migrationBuilder.DropColumn(
            name: "NumberOfPages",
            table: "Books");

        migrationBuilder.DropColumn(
            name: "PublishDate",
            table: "Books");

        migrationBuilder.DropColumn(
            name: "Publishers",
            table: "Books");
    }
}
