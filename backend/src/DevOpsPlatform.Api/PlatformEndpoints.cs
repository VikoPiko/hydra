using System.Text.Json;
using DevOpsPlatform.Application;
using DevOpsPlatform.Domain;
using DevOpsPlatform.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevOpsPlatform.Api;

public static partial class PlatformEndpoints
{
    public static void MapPlatformEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api");
        api.MapGet("/projects", async (PlatformDbContext db, CancellationToken ct) =>
            await db.Projects.AsNoTracking().OrderBy(x => x.Name).ToListAsync(ct));
        api.MapGet("/projects/{projectId:guid}", async (Guid projectId, PlatformDbContext db, CancellationToken ct) =>
            await db.Projects.AsNoTracking().FirstOrDefaultAsync(x => x.Id == projectId, ct) is { } project
                ? Results.Ok(project) : Results.NotFound());
        api.MapPost("/projects", async (CreateProject request, PlatformDbContext db, CancellationToken ct) =>
        {
            if (!ValidName(request.Name)) return Bad("Name must be 2–120 characters.");
            if (request.Description?.Length > 2000) return Bad("Description is too long.");
            if (await db.Projects.AnyAsync(x => x.Name == request.Name.Trim(), ct)) return Conflict("Project name already exists.");
            var project = new Project { Name = request.Name.Trim(), Description = request.Description?.Trim() ?? "" };
            db.Projects.Add(project);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/projects/{project.Id}", project);
        });

        api.MapGet("/projects/{projectId:guid}/environments", async (Guid projectId, PlatformDbContext db, CancellationToken ct) =>
            await db.Environments.AsNoTracking().Where(x => x.ProjectId == projectId).OrderBy(x => x.Name).ToListAsync(ct));
        api.MapPost("/projects/{projectId:guid}/environments", async (Guid projectId, CreateEnvironment request, PlatformDbContext db, CancellationToken ct) =>
        {
            if (!await db.Projects.AnyAsync(x => x.Id == projectId, ct)) return Results.NotFound();
            if (!ValidName(request.Name) || !Enum.IsDefined(request.Type)) return Bad("Invalid environment name or type.");
            if (await db.Environments.AnyAsync(x => x.ProjectId == projectId && x.Name == request.Name.Trim(), ct)) return Conflict("Environment name already exists.");
            var environment = new SystemEnvironment { ProjectId = projectId, Name = request.Name.Trim(), Type = request.Type };
            db.Environments.Add(environment);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/projects/{projectId}/environments/{environment.Id}", environment);
        });

        api.MapGet("/projects/{projectId:guid}/components", async (Guid projectId, PlatformDbContext db, CancellationToken ct) =>
            await db.Components.AsNoTracking().Where(x => x.ProjectId == projectId).OrderBy(x => x.Name).ToListAsync(ct));
        api.MapGet("/components/{componentId:guid}", async (Guid componentId, PlatformDbContext db, CancellationToken ct) =>
            await db.Components.AsNoTracking().FirstOrDefaultAsync(x => x.Id == componentId, ct) is { } component
                ? Results.Ok(component) : Results.NotFound());
        api.MapPost("/projects/{projectId:guid}/components", async (Guid projectId, CreateComponent request, PlatformDbContext db, CancellationToken ct) =>
        {
            if (!await db.Projects.AnyAsync(x => x.Id == projectId, ct)) return Results.NotFound();
            if (!ValidName(request.Name) || !Enum.IsDefined(request.Type) || request.Description?.Length > 2000 || request.Owner?.Length > 120)
                return Bad("Invalid component fields.");
            if (request.RepositoryUrl is { } url && (url.Length > 500 || !Uri.TryCreate(url, UriKind.Absolute, out var parsed) || (parsed.Scheme != "https" && parsed.Scheme != "http")))
                return Bad("Repository URL must be an absolute HTTP(S) URL.");
            if (await db.Components.AnyAsync(x => x.ProjectId == projectId && x.Name == request.Name.Trim(), ct)) return Conflict("Component name already exists.");
            var component = new SystemComponent { ProjectId = projectId, Name = request.Name.Trim(), Description = request.Description?.Trim() ?? "",
                Type = request.Type, RepositoryUrl = request.RepositoryUrl, Owner = request.Owner?.Trim() };
            db.Components.Add(component);
            db.OperationalEvents.Add(new OperationalEvent { ProjectId = projectId, ComponentId = component.Id,
                Type = OperationalEventType.ComponentRegistered, Severity = EventSeverity.Info, Title = $"{component.Name} registered" });
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/components/{component.Id}", component);
        });

        api.MapGet("/projects/{projectId:guid}/dependencies", async (Guid projectId, PlatformDbContext db, CancellationToken ct) =>
            await db.Dependencies.AsNoTracking().Where(x => x.ProjectId == projectId).ToListAsync(ct));
        api.MapPost("/projects/{projectId:guid}/dependencies", async (Guid projectId, CreateDependency request, PlatformDbContext db, CancellationToken ct) =>
        {
            if (!await db.Projects.AnyAsync(x => x.Id == projectId, ct)) return Results.NotFound();
            if (!Dependency.IsValidPair(request.SourceComponentId, request.TargetComponentId) || !Enum.IsDefined(request.Type) || !Enum.IsDefined(request.Source))
                return Bad("Invalid dependency.");
            var count = await db.Components.CountAsync(x => x.ProjectId == projectId &&
                (x.Id == request.SourceComponentId || x.Id == request.TargetComponentId), ct);
            if (count != 2) return Bad("Both components must belong to the project.");
            if (await db.Dependencies.AnyAsync(x => x.SourceComponentId == request.SourceComponentId &&
                x.TargetComponentId == request.TargetComponentId && x.Type == request.Type, ct)) return Conflict("Dependency already exists.");
            var dependency = new Dependency { ProjectId = projectId, SourceComponentId = request.SourceComponentId,
                TargetComponentId = request.TargetComponentId, Type = request.Type, Source = request.Source };
            db.Dependencies.Add(dependency);
            db.OperationalEvents.Add(new OperationalEvent { ProjectId = projectId, ComponentId = request.SourceComponentId,
                Type = OperationalEventType.DependencyCreated, Severity = EventSeverity.Info, Title = "Dependency registered" });
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/projects/{projectId}/dependencies/{dependency.Id}", dependency);
        });

        api.MapGet("/projects/{projectId:guid}/environments/{environmentId:guid}/topology", async (Guid projectId, Guid environmentId, PlatformDbContext db, CancellationToken ct) =>
        {
            if (!await ValidEnvironment(db, projectId, environmentId, ct)) return Results.NotFound();
            var components = await db.Components.AsNoTracking().Where(x => x.ProjectId == projectId).OrderBy(x => x.Name).ToListAsync(ct);
            var dependencies = await db.Dependencies.AsNoTracking().Where(x => x.ProjectId == projectId).ToListAsync(ct);
            var states = await db.RuntimeStates.AsNoTracking().Where(x => x.EnvironmentId == environmentId).ToDictionaryAsync(x => x.ComponentId, ct);
            var deployments = await db.Deployments.AsNoTracking().Where(x => x.EnvironmentId == environmentId && x.Status == DeploymentStatus.Succeeded)
                .OrderByDescending(x => x.CompletedAt).ToListAsync(ct);
            var versions = deployments.GroupBy(x => x.ComponentId).ToDictionary(x => x.Key, x => x.First().Version);
            var nodes = components.Select(x =>
            {
                states.TryGetValue(x.Id, out var state);
                versions.TryGetValue(x.Id, out var version);
                return new TopologyNode(x.Id, x.Name, x.Type, state?.HealthStatus ?? HealthStatus.Unknown, version,
                    x.Description, x.RepositoryUrl, x.Owner, new TopologyMetrics(state?.CpuPercent, state?.MemoryPercent,
                        state?.RequestRate, state?.ErrorRatePercent, state?.LatencyP95Ms));
            }).ToArray();
            return Results.Ok(new TopologyResponse(projectId, environmentId, DateTimeOffset.UtcNow, nodes,
                dependencies.Select(x => new TopologyEdge(x.Id, x.SourceComponentId, x.TargetComponentId, x.Type, x.Source)).ToArray()));
        });
        api.MapGet("/projects/{projectId:guid}/components/{componentId:guid}/impact", async (Guid projectId, Guid componentId, PlatformDbContext db, CancellationToken ct) =>
        {
            var components = await db.Components.AsNoTracking().Where(x => x.ProjectId == projectId).ToListAsync(ct);
            if (components.All(x => x.Id != componentId)) return Results.NotFound();
            var dependencies = await db.Dependencies.AsNoTracking().Where(x => x.ProjectId == projectId).ToListAsync(ct);
            return Results.Ok(ImpactAnalysis.Analyze(componentId, components, dependencies));
        });

        MapLifecycleEndpoints(api, app.Environment.IsDevelopment());
    }

    private static bool ValidName(string? name) => name?.Trim().Length is >= 2 and <= 120;
    private static IResult Bad(string detail) => Results.Problem(detail: detail, statusCode: 400);
    private static IResult Conflict(string detail) => Results.Problem(detail: detail, statusCode: 409);
    private static Task<bool> ValidEnvironment(PlatformDbContext db, Guid projectId, Guid environmentId, CancellationToken ct) =>
        db.Environments.AnyAsync(x => x.Id == environmentId && x.ProjectId == projectId, ct);
}
