using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations;

/// <inheritdoc />
public partial class AddIsbnBedethequeCacheTable : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "IsbnBedethequeUrls",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ISBN = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                Url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_IsbnBedethequeUrls", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_IsbnBedethequeUrls_ISBN",
            table: "IsbnBedethequeUrls",
            column: "ISBN",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "IsbnBedethequeUrls");
    }
}
