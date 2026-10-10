using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations;

/// <inheritdoc />
public partial class AddFeatureToggleOverrides : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "FeatureToggleOverrides",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Toggle = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Enabled = table.Column<bool>(type: "boolean", nullable: false),
                CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FeatureToggleOverrides", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_FeatureToggleOverrides_Toggle",
            table: "FeatureToggleOverrides",
            column: "Toggle",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "FeatureToggleOverrides");
    }
}
