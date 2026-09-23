using System.Security.Claims;
using System.Text.Json;
using DevOpsPlatform.Application;
using DevOpsPlatform.Domain;
using DevOpsPlatform.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevOpsPlatform.Api;

public sealed record CreateRepositoryConnection(RepositoryProvider Provider, string Name, string RepositoryUrl, string? DefaultBranch);
public sealed record CreateDemoImport(string Name, string? Description, string? Slug, ProjectVisibility Visibility);
public sealed record UpdateDiscoveryOverride(string? Name, ComponentType? Type);

public static partial class PlatformEndpoints
{
    private static void MapRepositoryDiscoveryEndpoints(RouteGroupBuilder api)
    {
        api.MapPost("/projects/import-demo", async (CreateDemoImport request, ClaimsPrincipal principal,
            ProjectAccess access, PlatformDbContext db, CancellationToken ct) =>
        {
            if (!ValidName(request.Name) || request.Description?.Length > 2000 || !Enum.IsDefined(request.Visibility))
                return Bad("Invalid project fields.");
            var slug = request.Slug is null ? Slugify(request.Name) : request.Slug.Trim();
            if (!ValidSlug(slug)) return Bad("Invalid project slug.");
            if (await db.Projects.AnyAsync(x => x.Name == request.Name.Trim() || x.Slug == slug, ct))
                return Conflict("Project name or slug already exists.");
            var user = await access.GetUserAsync(principal, ct);
            await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(ct) : null;
            var project = new Project { Name = request.Name.Trim(), Description = request.Description?.Trim() ?? "",
                Slug = slug, Visibility = request.Visibility, CreatedByUserId = user.Id };
            var environment = new SystemEnvironment { ProjectId = project.Id, Name = "Production", Type = EnvironmentType.Production };
            var connection = new RepositoryConnection { ProjectId = project.Id, Provider = RepositoryProvider.LocalDevelopment,
                Name = "Bundled Commerce System", RepositoryUrl = "demo://commerce-system", DefaultBranch = "bundled" };
            var scan = new RepositoryScan { RepositoryConnectionId = connection.Id, Branch = connection.DefaultBranch };
            var job = new SynchronizationJob { ProjectId = project.Id, RepositoryConnectionId = connection.Id,
                ScanId = scan.Id, Type = SynchronizationJobType.RepositoryScan, RequestedByUserId = user.Id };
            db.Projects.Add(project);
            db.ProjectMembers.Add(new ProjectMember { ProjectId = project.Id, UserId = user.Id, Role = ProjectRole.Owner });
            db.Environments.Add(environment);
            db.RepositoryConnections.Add(connection);
            db.RepositoryScans.Add(scan);
            db.SynchronizationJobs.Add(job);
            await db.SaveChangesAsync(ct);
            if (transaction is not null) await transaction.CommitAsync(ct);
            return Results.Accepted($"/api/projects/{project.Id}/repository-connections/{connection.Id}/scans/{scan.Id}",
                new { projectId = project.Id, environmentId = environment.Id, connectionId = connection.Id, scanId = scan.Id, jobId = job.Id });
        });

        api.MapGet("/projects/{projectId:guid}/repository-connections", async (Guid projectId, PlatformDbContext db, CancellationToken ct) =>
            await db.RepositoryConnections.AsNoTracking().Where(x => x.ProjectId == projectId)
                .OrderBy(x => x.Name).ToListAsync(ct));

        api.MapPost("/projects/{projectId:guid}/repository-connections", async (Guid projectId, CreateRepositoryConnection request,
            PlatformDbContext db, CancellationToken ct) =>
        {
            if (!await db.Projects.AnyAsync(x => x.Id == projectId && x.ArchivedAt == null, ct)) return Results.NotFound();
            if (request.Provider != RepositoryProvider.LocalDevelopment || request.RepositoryUrl != "demo://commerce-system")
                return Bad("This slice supports the bundled Commerce System repository only.");
            if (!ValidName(request.Name) || request.DefaultBranch?.Length > 200) return Bad("Invalid repository name or branch.");
            if (await db.RepositoryConnections.AnyAsync(x => x.ProjectId == projectId && x.RepositoryUrl == request.RepositoryUrl, ct))
                return Conflict("Repository is already connected to this project.");
            var connection = new RepositoryConnection { ProjectId = projectId, Provider = request.Provider,
                Name = request.Name.Trim(), RepositoryUrl = request.RepositoryUrl,
                DefaultBranch = request.DefaultBranch?.Trim() is { Length: > 0 } branch ? branch : "bundled" };
            db.RepositoryConnections.Add(connection);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/projects/{projectId}/repository-connections/{connection.Id}", connection);
        });

        api.MapPost("/projects/{projectId:guid}/repository-connections/{connectionId:guid}/disconnect",
            async (Guid projectId, Guid connectionId, PlatformDbContext db, CancellationToken ct) =>
        {
            var connection = await db.RepositoryConnections.FirstOrDefaultAsync(x => x.Id == connectionId && x.ProjectId == projectId, ct);
            if (connection is null) return Results.NotFound();
            if (await db.SynchronizationJobs.AnyAsync(x => x.RepositoryConnectionId == connectionId &&
                (x.Status == SynchronizationJobStatus.Queued || x.Status == SynchronizationJobStatus.Running), ct))
                return Conflict("Wait for the active scan before disconnecting.");
            connection.Status = RepositoryConnectionStatus.Disconnected;
            connection.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.Ok(connection);
        });

        api.MapPost("/projects/{projectId:guid}/repository-connections/{connectionId:guid}/reconnect",
            async (Guid projectId, Guid connectionId, PlatformDbContext db, CancellationToken ct) =>
        {
            var connection = await db.RepositoryConnections.FirstOrDefaultAsync(x => x.Id == connectionId && x.ProjectId == projectId, ct);
            if (connection is null) return Results.NotFound();
            connection.Status = RepositoryConnectionStatus.Unknown;
            connection.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.Ok(connection);
        });

        api.MapPost("/projects/{projectId:guid}/repository-connections/{connectionId:guid}/scans", async (Guid projectId,
            Guid connectionId, ClaimsPrincipal principal, ProjectAccess access, PlatformDbContext db, CancellationToken ct) =>
        {
            var connection = await db.RepositoryConnections.FirstOrDefaultAsync(x => x.Id == connectionId && x.ProjectId == projectId, ct);
            if (connection is null) return Results.NotFound();
            if (connection.Status == RepositoryConnectionStatus.Disconnected) return Conflict("Reconnect this repository before scanning.");
            if (await db.SynchronizationJobs.AnyAsync(x => x.RepositoryConnectionId == connectionId &&
                x.Type == SynchronizationJobType.RepositoryScan &&
                (x.Status == SynchronizationJobStatus.Queued || x.Status == SynchronizationJobStatus.Running), ct))
                return Conflict("A scan is already queued or running for this repository.");
            var user = await access.GetUserAsync(principal, ct);
            var scan = new RepositoryScan { RepositoryConnectionId = connectionId, Branch = connection.DefaultBranch };
            var job = new SynchronizationJob { ProjectId = projectId, RepositoryConnectionId = connectionId,
                ScanId = scan.Id, Type = SynchronizationJobType.RepositoryScan, RequestedByUserId = user.Id };
            db.RepositoryScans.Add(scan);
            db.SynchronizationJobs.Add(job);
            await db.SaveChangesAsync(ct);
            return Results.Accepted($"/api/projects/{projectId}/repository-connections/{connectionId}/scans/{scan.Id}",
                new { scan.Id, jobId = job.Id, scan.Status });
        });

        api.MapGet("/projects/{projectId:guid}/repository-connections/{connectionId:guid}/scans", async (Guid projectId,
            Guid connectionId, int? limit, PlatformDbContext db, CancellationToken ct) =>
        {
            if (limit is < 1 or > 100) return Bad("Limit must be 1–100.");
            if (!await db.RepositoryConnections.AnyAsync(x => x.Id == connectionId && x.ProjectId == projectId, ct)) return Results.NotFound();
            return Results.Ok(await db.RepositoryScans.AsNoTracking().Where(x => x.RepositoryConnectionId == connectionId)
                .OrderByDescending(x => x.StartedAt).Take(limit ?? 25).Select(x => new
                {
                    x.Id, x.RepositoryConnectionId, x.CommitSha, x.Branch, x.Status, x.StartedAt, x.CompletedAt,
                    x.FilesScanned, x.ComponentsDiscovered, x.DependenciesDiscovered, x.WarningsCount, x.ErrorMessage, x.AppliedAt
                }).ToListAsync(ct));
        });

        api.MapGet("/projects/{projectId:guid}/repository-connections/{connectionId:guid}/scans/{scanId:guid}",
            async (Guid projectId, Guid connectionId, Guid scanId, PlatformDbContext db, CancellationToken ct) =>
        {
            var scan = await db.RepositoryScans.AsNoTracking().FirstOrDefaultAsync(x => x.Id == scanId &&
                x.RepositoryConnectionId == connectionId && db.RepositoryConnections.Any(c => c.Id == connectionId && c.ProjectId == projectId), ct);
            return scan is null ? Results.NotFound() : Results.Ok(new
            {
                scan.Id, scan.RepositoryConnectionId, scan.CommitSha, scan.Branch, scan.Status, scan.StartedAt, scan.CompletedAt,
                scan.FilesScanned, scan.ComponentsDiscovered, scan.DependenciesDiscovered, scan.WarningsCount, scan.ErrorMessage, scan.AppliedAt
            });
        });

        api.MapGet("/projects/{projectId:guid}/repository-connections/{connectionId:guid}/scans/{scanId:guid}/preview",
            async (Guid projectId, Guid connectionId, Guid scanId, PlatformDbContext db, CancellationToken ct) =>
        {
            var scan = await db.RepositoryScans.AsNoTracking().FirstOrDefaultAsync(x => x.Id == scanId &&
                x.RepositoryConnectionId == connectionId && db.RepositoryConnections.Any(c => c.Id == connectionId && c.ProjectId == projectId), ct);
            if (scan is null) return Results.NotFound();
            if (scan.PreviewJson is null) return Conflict("Scan preview is not ready.");
            var discovery = JsonSerializer.Deserialize<DiscoveryResult>(scan.PreviewJson, DiscoveryJson.Options)!;
            var existing = await db.Components.AsNoTracking().Where(x => x.ProjectId == projectId).ToListAsync(ct);
            var keys = discovery.Components.Select(x => DiscoveryKey(x)).ToHashSet(StringComparer.Ordinal);
            var newComponents = new List<DiscoveredComponent>();
            var changedComponents = new List<DiscoveredComponent>();
            var conflicts = new List<DiscoveredComponent>();
            foreach (var candidate in discovery.Components)
            {
                var matched = existing.FirstOrDefault(x => x.RepositoryConnectionId == connectionId && x.ExternalIdentifier == DiscoveryKey(candidate));
                if (matched is null)
                {
                    if (existing.Any(x => x.Name == candidate.Name)) conflicts.Add(candidate);
                    else newComponents.Add(candidate);
                }
                else if ((!matched.IsNameOverridden && matched.Name != candidate.Name) ||
                    (!matched.IsTypeOverridden && matched.Type != candidate.Type) || matched.DiscoveryStatus != DiscoveryStatus.Active)
                    changedComponents.Add(candidate);
            }
            var missing = existing.Where(x => x.RepositoryConnectionId == connectionId && x.ExternalIdentifier is not null &&
                !keys.Contains(x.ExternalIdentifier)).Select(x => new { x.Id, x.Name, x.ExternalIdentifier }).ToArray();
            return Results.Ok(new { scan.Id, scan.Status, scan.AppliedAt, discovery.Components, discovery.Dependencies,
                discovery.Warnings, NewComponents = newComponents, ChangedComponents = changedComponents,
                MissingComponents = missing, Conflicts = conflicts });
        });

        api.MapPost("/projects/{projectId:guid}/repository-connections/{connectionId:guid}/scans/{scanId:guid}/apply",
            async (Guid projectId, Guid connectionId, Guid scanId, PlatformDbContext db, CancellationToken ct) =>
        {
            var scan = await db.RepositoryScans.FirstOrDefaultAsync(x => x.Id == scanId && x.RepositoryConnectionId == connectionId &&
                db.RepositoryConnections.Any(c => c.Id == connectionId && c.ProjectId == projectId), ct);
            if (scan is null) return Results.NotFound();
            if (scan.AppliedAt is not null) return Results.Ok(new { alreadyApplied = true, componentsCreated = 0, dependenciesCreated = 0, conflicts = 0 });
            if (scan.Status is not (RepositoryScanStatus.Succeeded or RepositoryScanStatus.PartiallySucceeded) || scan.PreviewJson is null)
                return Conflict("Only completed scans can be applied.");
            var discovery = JsonSerializer.Deserialize<DiscoveryResult>(scan.PreviewJson, DiscoveryJson.Options)!;
            await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(ct) : null;
            var now = DateTimeOffset.UtcNow;
            var components = await db.Components.Where(x => x.ProjectId == projectId).ToListAsync(ct);
            var byKey = components.Where(x => x.RepositoryConnectionId == connectionId && x.ExternalIdentifier is not null)
                .ToDictionary(x => x.ExternalIdentifier!, StringComparer.Ordinal);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var resolved = new Dictionary<string, SystemComponent>(StringComparer.Ordinal);
            var created = 0;
            var conflicts = 0;
            foreach (var candidate in discovery.Components)
            {
                var key = DiscoveryKey(candidate);
                if (!seen.Add(key)) { conflicts++; continue; }
                if (!byKey.TryGetValue(key, out var component))
                {
                    if (components.Any(x => x.Name == candidate.Name)) { conflicts++; continue; }
                    component = new SystemComponent { ProjectId = projectId, Name = candidate.Name, Type = candidate.Type,
                        Origin = ComponentOrigin.Discovered, RepositoryConnectionId = connectionId,
                        ExternalIdentifier = key, LastDiscoveredAt = now };
                    db.Components.Add(component);
                    components.Add(component);
                    created++;
                }
                else
                {
                    if (!component.IsNameOverridden && component.Name != candidate.Name &&
                        !components.Any(x => x.Id != component.Id && x.Name == candidate.Name)) component.Name = candidate.Name;
                    if (!component.IsTypeOverridden) component.Type = candidate.Type;
                    component.DiscoveryStatus = DiscoveryStatus.Active;
                    component.LastDiscoveredAt = now;
                    component.UpdatedAt = now;
                }
                resolved[candidate.ExternalKey] = component;
            }
            foreach (var component in components.Where(x => x.RepositoryConnectionId == connectionId && x.ExternalIdentifier is not null &&
                !seen.Contains(x.ExternalIdentifier))) component.DiscoveryStatus = DiscoveryStatus.Missing;
            var existingEdges = await db.Dependencies.Where(x => x.ProjectId == projectId).ToListAsync(ct);
            var existingEvidence = await db.DependencyEvidence.Where(x => x.RepositoryConnectionId == connectionId).ToListAsync(ct);
            var edgesCreated = 0;
            foreach (var candidate in discovery.Dependencies)
            {
                if (!resolved.TryGetValue(candidate.SourceExternalKey, out var source) ||
                    !resolved.TryGetValue(candidate.TargetExternalKey, out var target)) continue;
                var edge = existingEdges.FirstOrDefault(x => x.SourceComponentId == source.Id && x.TargetComponentId == target.Id &&
                    x.Type == candidate.Type && x.Source == DependencySource.Discovered);
                if (edge is null)
                {
                    edge = new Dependency { ProjectId = projectId, SourceComponentId = source.Id, TargetComponentId = target.Id,
                        Type = candidate.Type, Source = DependencySource.Discovered };
                    db.Dependencies.Add(edge);
                    existingEdges.Add(edge);
                    edgesCreated++;
                }
                if (!existingEvidence.Any(x => x.DependencyId == edge.Id && x.Type == DiscoveryEvidenceType.DockerCompose &&
                    x.SourcePath == candidate.SourcePath && x.SourceLocation == candidate.SourceLocation))
                {
                    var evidence = new DependencyEvidence { DependencyId = edge.Id, Type = DiscoveryEvidenceType.DockerCompose,
                        SourceKind = DependencySource.Discovered, RepositoryConnectionId = connectionId, ScanId = scan.Id,
                        SourcePath = candidate.SourcePath, SourceLocation = candidate.SourceLocation,
                        Description = candidate.Description };
                    db.DependencyEvidence.Add(evidence);
                    existingEvidence.Add(evidence);
                }
            }
            scan.AppliedAt = now;
            await db.SaveChangesAsync(ct);
            if (transaction is not null) await transaction.CommitAsync(ct);
            return Results.Ok(new { alreadyApplied = false, componentsCreated = created, dependenciesCreated = edgesCreated, conflicts });
        });

        api.MapPatch("/components/{componentId:guid}/discovery-override", async (Guid componentId,
            UpdateDiscoveryOverride request, PlatformDbContext db, CancellationToken ct) =>
        {
            var component = await db.Components.FindAsync([componentId], ct);
            if (component is null) return Results.NotFound();
            if (component.Origin != ComponentOrigin.Discovered) return Bad("Only discovered components support discovery overrides.");
            if (request.Name is null && request.Type is null) return Bad("Provide a name or type override.");
            if (request.Name is not null)
            {
                if (!ValidName(request.Name)) return Bad("Name must be 2–120 characters.");
                var name = request.Name.Trim();
                if (await db.Components.AnyAsync(x => x.ProjectId == component.ProjectId && x.Id != componentId && x.Name == name, ct))
                    return Conflict("Component name already exists.");
                component.Name = name;
                component.IsNameOverridden = true;
            }
            if (request.Type is { } type)
            {
                if (!Enum.IsDefined(type)) return Bad("Invalid component type.");
                component.Type = type;
                component.IsTypeOverridden = true;
            }
            component.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.Ok(component);
        });
    }

    private static string DiscoveryKey(DiscoveredComponent candidate) => $"{candidate.SourcePath}:{candidate.ExternalKey}";
}
