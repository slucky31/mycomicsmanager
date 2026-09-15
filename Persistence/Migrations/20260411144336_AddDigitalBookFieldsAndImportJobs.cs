using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations;

/// <inheritdoc />
public partial class AddDigitalBookFieldsAndImportJobs : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "FilePath",
            table: "DigitalBooks",
            type: "character varying(500)",
            maxLength: 500,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<long>(
            name: "FileSize",
            table: "DigitalBooks",
            type: "bigint",
            nullable: false,
            defaultValue: 0L);

        migrationBuilder.CreateTable(
            name: "ImportJobs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OriginalFileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                OriginalFilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                OriginalFileSize = table.Column<long>(type: "bigint", nullable: false),
                LibraryId = table.Column<Guid>(type: "uuid", nullable: false),
                DigitalBookId = table.Column<Guid>(type: "uuid", nullable: true),
                Status = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                ErrorStep = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_ImportJobs", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_ImportJobs_LibraryId",
            table: "ImportJobs",
            column: "LibraryId");

        migrationBuilder.CreateIndex(
            name: "IX_ImportJobs_Status",
            table: "ImportJobs",
            column: "Status");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ImportJobs");

        migrationBuilder.DropColumn(
            name: "FilePath",
            table: "DigitalBooks");

        migrationBuilder.DropColumn(
            name: "FileSize",
            table: "DigitalBooks");
    }
}
