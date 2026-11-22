using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations;

/// <inheritdoc />
public partial class AddAutoRetryDownloads : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AutoRetryDownloads",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                DecisionId = table.Column<Guid>(type: "uuid", nullable: false),
                AutoRetryCount = table.Column<int>(type: "integer", nullable: false),
                DownloadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AutoRetryDownloads", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AutoRetryDownloads_UserId_DownloadedAt",
            table: "AutoRetryDownloads",
            columns: new[] { "UserId", "DownloadedAt" });

        // The feeds already downloaded after an automatic retry (Downloaded = 5) keep being counted.
        migrationBuilder.Sql("""
            INSERT INTO "AutoRetryDownloads" ("Id", "UserId", "DecisionId", "AutoRetryCount", "DownloadedAt", "CreatedOnUtc")
            SELECT gen_random_uuid(), "UserId", "Id", "AutoRetryCount", "UpdatedAt", now()
            FROM "FeedImportDecisions"
            WHERE "Status" = 5 AND "AutoRetryCount" > 0;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AutoRetryDownloads");
    }
}
