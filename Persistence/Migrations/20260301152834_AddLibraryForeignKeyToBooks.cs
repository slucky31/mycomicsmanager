using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations;

/// <inheritdoc />
public partial class AddLibraryForeignKeyToBooks : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_Books_LibraryId",
            table: "Books",
            column: "LibraryId");

        migrationBuilder.AddForeignKey(
            name: "FK_Books_Libraries_LibraryId",
            table: "Books",
            column: "LibraryId",
            principalTable: "Libraries",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Books_Libraries_LibraryId",
            table: "Books");

        migrationBuilder.DropIndex(
            name: "IX_Books_LibraryId",
            table: "Books");
    }
}
