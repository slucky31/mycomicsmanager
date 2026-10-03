using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations;

/// <inheritdoc />
public partial class AddFeedImportItemsAndArbitration : Migration
{
    private static readonly string[] s_entryItemColumns = ["UserId", "MinifluxEntryId", "ItemIndex"];
    private static readonly string[] s_entryColumns = ["UserId", "MinifluxEntryId"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_FeedImportDecisions_UserId_MinifluxEntryId",
            table: "FeedImportDecisions");

        migrationBuilder.AddColumn<int>(
            name: "ArbitrationKind",
            table: "FeedImportDecisions",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "ItemIndex",
            table: "FeedImportDecisions",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.CreateIndex(
            name: "IX_FeedImportDecisions_UserId_MinifluxEntryId_ItemIndex",
            table: "FeedImportDecisions",
            columns: s_entryItemColumns,
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_FeedImportDecisions_UserId_MinifluxEntryId_ItemIndex",
            table: "FeedImportDecisions");

        migrationBuilder.DropColumn(
            name: "ArbitrationKind",
            table: "FeedImportDecisions");

        migrationBuilder.DropColumn(
            name: "ItemIndex",
            table: "FeedImportDecisions");

        migrationBuilder.CreateIndex(
            name: "IX_FeedImportDecisions_UserId_MinifluxEntryId",
            table: "FeedImportDecisions",
            columns: s_entryColumns,
            unique: true);
    }
}
