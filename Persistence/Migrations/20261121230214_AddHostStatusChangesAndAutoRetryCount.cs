using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations;

/// <inheritdoc />
public partial class AddHostStatusChangesAndAutoRetryCount : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "AutoRetryCount",
            table: "FeedImportDecisions",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.CreateTable(
            name: "HostStatusChanges",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Host = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                IsUp = table.Column<bool>(type: "boolean", nullable: false),
                ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_HostStatusChanges", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_HostStatusChanges_Host_ChangedAt",
            table: "HostStatusChanges",
            columns: new[] { "Host", "ChangedAt" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "HostStatusChanges");

        migrationBuilder.DropColumn(
            name: "AutoRetryCount",
            table: "FeedImportDecisions");
    }
}
