using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevOpsPlatform.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RepositoryDiscoveryFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DiscoveryStatus",
                table: "Components",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.AddColumn<string>(
                name: "ExternalIdentifier",
                table: "Components",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsNameOverridden",
                table: "Components",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTypeOverridden",
                table: "Components",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastDiscoveredAt",
                table: "Components",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Origin",
                table: "Components",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Manual");

            migrationBuilder.AddColumn<Guid>(
                name: "RepositoryConnectionId",
                table: "Components",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RepositoryConnections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    RepositoryUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    DefaultBranch = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ExternalRepositoryId = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    AuthenticationMode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LastSuccessfulSyncAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastSyncAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastSyncError = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepositoryConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepositoryConnections_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RepositoryScans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RepositoryConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CommitSha = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Branch = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FilesScanned = table.Column<int>(type: "integer", nullable: false),
                    ComponentsDiscovered = table.Column<int>(type: "integer", nullable: false),
                    DependenciesDiscovered = table.Column<int>(type: "integer", nullable: false),
                    WarningsCount = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PreviewJson = table.Column<string>(type: "text", nullable: true),
                    AppliedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepositoryScans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepositoryScans_RepositoryConnections_RepositoryConnectionId",
                        column: x => x.RepositoryConnectionId,
                        principalTable: "RepositoryConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DependencyEvidence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DependencyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SourceKind = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    RepositoryConnectionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ScanId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourcePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SourceLocation = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ObservedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DependencyEvidence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DependencyEvidence_Dependencies_DependencyId",
                        column: x => x.DependencyId,
                        principalTable: "Dependencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DependencyEvidence_RepositoryConnections_RepositoryConnecti~",
                        column: x => x.RepositoryConnectionId,
                        principalTable: "RepositoryConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DependencyEvidence_RepositoryScans_ScanId",
                        column: x => x.ScanId,
                        principalTable: "RepositoryScans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SynchronizationJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    RepositoryConnectionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ScanId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SynchronizationJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SynchronizationJobs_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SynchronizationJobs_RepositoryConnections_RepositoryConnect~",
                        column: x => x.RepositoryConnectionId,
                        principalTable: "RepositoryConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SynchronizationJobs_RepositoryScans_ScanId",
                        column: x => x.ScanId,
                        principalTable: "RepositoryScans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SynchronizationJobs_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Components_RepositoryConnectionId_ExternalIdentifier",
                table: "Components",
                columns: new[] { "RepositoryConnectionId", "ExternalIdentifier" },
                unique: true,
                filter: "\"RepositoryConnectionId\" IS NOT NULL AND \"ExternalIdentifier\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DependencyEvidence_DependencyId_Type",
                table: "DependencyEvidence",
                columns: new[] { "DependencyId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_DependencyEvidence_RepositoryConnectionId",
                table: "DependencyEvidence",
                column: "RepositoryConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_DependencyEvidence_ScanId",
                table: "DependencyEvidence",
                column: "ScanId");

            migrationBuilder.CreateIndex(
                name: "IX_RepositoryConnections_ProjectId_RepositoryUrl",
                table: "RepositoryConnections",
                columns: new[] { "ProjectId", "RepositoryUrl" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RepositoryScans_RepositoryConnectionId_StartedAt",
                table: "RepositoryScans",
                columns: new[] { "RepositoryConnectionId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SynchronizationJobs_ProjectId_CreatedAt",
                table: "SynchronizationJobs",
                columns: new[] { "ProjectId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SynchronizationJobs_RepositoryConnectionId",
                table: "SynchronizationJobs",
                column: "RepositoryConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_SynchronizationJobs_RequestedByUserId",
                table: "SynchronizationJobs",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SynchronizationJobs_ScanId",
                table: "SynchronizationJobs",
                column: "ScanId");

            migrationBuilder.CreateIndex(
                name: "IX_SynchronizationJobs_Status_CreatedAt",
                table: "SynchronizationJobs",
                columns: new[] { "Status", "CreatedAt" });

            // Preserve the V2 source classification as explainable evidence before new scans run.
            migrationBuilder.Sql("""
                INSERT INTO "DependencyEvidence" ("Id", "DependencyId", "Type", "SourceKind", "ObservedAt", "CreatedAt")
                SELECT gen_random_uuid(), d."Id",
                    CASE d."Source" WHEN 'Declared' THEN 'Manual' WHEN 'Observed' THEN 'RuntimeObservation' ELSE 'Other' END,
                    d."Source", d."LastObservedAt", d."CreatedAt"
                FROM "Dependencies" d;
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_Components_RepositoryConnections_RepositoryConnectionId",
                table: "Components",
                column: "RepositoryConnectionId",
                principalTable: "RepositoryConnections",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Components_RepositoryConnections_RepositoryConnectionId",
                table: "Components");

            migrationBuilder.DropTable(
                name: "DependencyEvidence");

            migrationBuilder.DropTable(
                name: "SynchronizationJobs");

            migrationBuilder.DropTable(
                name: "RepositoryScans");

            migrationBuilder.DropTable(
                name: "RepositoryConnections");

            migrationBuilder.DropIndex(
                name: "IX_Components_RepositoryConnectionId_ExternalIdentifier",
                table: "Components");

            migrationBuilder.DropColumn(
                name: "DiscoveryStatus",
                table: "Components");

            migrationBuilder.DropColumn(
                name: "ExternalIdentifier",
                table: "Components");

            migrationBuilder.DropColumn(
                name: "IsNameOverridden",
                table: "Components");

            migrationBuilder.DropColumn(
                name: "IsTypeOverridden",
                table: "Components");

            migrationBuilder.DropColumn(
                name: "LastDiscoveredAt",
                table: "Components");

            migrationBuilder.DropColumn(
                name: "Origin",
                table: "Components");

            migrationBuilder.DropColumn(
                name: "RepositoryConnectionId",
                table: "Components");
        }
    }
}
