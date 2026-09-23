using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevOpsPlatform.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DependencyProvenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Dependencies_SourceComponentId_TargetComponentId_Type",
                table: "Dependencies");

            migrationBuilder.CreateIndex(
                name: "IX_Dependencies_SourceComponentId_TargetComponentId_Type_Source",
                table: "Dependencies",
                columns: new[] { "SourceComponentId", "TargetComponentId", "Type", "Source" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Dependencies_SourceComponentId_TargetComponentId_Type_Source",
                table: "Dependencies");

            migrationBuilder.CreateIndex(
                name: "IX_Dependencies_SourceComponentId_TargetComponentId_Type",
                table: "Dependencies",
                columns: new[] { "SourceComponentId", "TargetComponentId", "Type" },
                unique: true);
        }
    }
}
