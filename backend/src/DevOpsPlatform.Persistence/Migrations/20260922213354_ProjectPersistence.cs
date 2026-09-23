using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevOpsPlatform.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProjectPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ArchivedAt",
                table: "Projects",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "Projects",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Projects",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Visibility",
                table: "Projects",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Private");

            // Existing V1 projects must receive unique slugs before the index is added.
            // The demo keeps its stable public slug; other rows receive an ID suffix.
            migrationBuilder.Sql("""
                UPDATE "Projects"
                SET "Slug" = CASE
                    WHEN "Id" = '10000000-0000-0000-0000-000000000001'::uuid THEN 'commerce-demo'
                    ELSE left(coalesce(nullif(btrim(lower(regexp_replace("Name", '[^a-zA-Z0-9]+', '-', 'g')), '-'), ''), 'project'), 70)
                         || '-' || left(replace("Id"::text, '-', ''), 8)
                END,
                "Visibility" = CASE
                    WHEN "Id" = '10000000-0000-0000-0000-000000000001'::uuid THEN 'Internal'
                    ELSE 'Private'
                END;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Slug",
                table: "Projects",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Projects_Slug",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Visibility",
                table: "Projects");
        }
    }
}
