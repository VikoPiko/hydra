using System.Text.RegularExpressions;
using System.Security.Claims;
using DevOpsPlatform.Application;
using DevOpsPlatform.Domain;
using DevOpsPlatform.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevOpsPlatform.Api;

public static partial class PlatformEndpoints
{
    public static void MapPlatformEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api").RequireAuthorization();
        api.AddEndpointFilter(async (context, next) =>
        {
            var http = context.HttpContext;
            var projectId = Guid.TryParse(http.Request.RouteValues["projectId"]?.ToString(), out var routeId) ? routeId : Guid.Empty;
            if (projectId == Guid.Empty && Guid.TryParse(http.Request.RouteValues["componentId"]?.ToString(), out var componentId))
            {
                var db = http.RequestServices.GetRequiredService<PlatformDbContext>();
                projectId = await db.Components.AsNoTracking().Where(x => x.Id == componentId).Select(x => x.ProjectId).FirstOrDefaultAsync(http.RequestAborted);
                if (projectId == Guid.Empty) return Results.NotFound();
            }
            if (projectId == Guid.Empty && Guid.TryParse(http.Request.RouteValues["workItemId"]?.ToString(), out var workItemId))
            {
                var db = http.RequestServices.GetRequiredService<PlatformDbContext>();
                projectId = await db.WorkItems.AsNoTracking().Where(x => x.Id == workItemId).Select(x => x.ProjectId).FirstOrDefaultAsync(http.RequestAborted);
                if (projectId == Guid.Empty) return Results.NotFound();
            }
            if (projectId != Guid.Empty)
            {
                var access = http.RequestServices.GetRequiredService<ProjectAccess>();
                var user = await access.GetUserAsync(http.User, http.RequestAborted);
                var path = http.Request.Path.Value ?? "";
                var required = http.Request.Method == "GET" ? ProjectRole.Viewer : ProjectRole.Contributor;
                if (path.EndsWith("/archive") ||
                    (http.Request.Method != "GET" && path.Contains("/members", StringComparison.Ordinal)) ||
                    (http.Request.Method == "PUT" && path.StartsWith("/api/projects/") && path.Count(c => c == '/') == 3))
                    required = ProjectRole.Owner;
                if (!ProjectAccess.Allows(await access.RoleAsync(projectId, user, http.RequestAborted), required))
                    return Results.Forbid();
            }
            return await next(context);
        });
        api.MapGet("/me", async (ClaimsPrincipal principal, ProjectAccess access, CancellationToken ct) =>
        {
            var user = await access.GetUserAsync(principal, ct);
            return Results.Ok(new { user.Id, user.DisplayName, user.Email });
        });
        api.MapGet("/projects", async (bool? includeArchived, ClaimsPrincipal principal, ProjectAccess access, PlatformDbContext db, CancellationToken ct) =>
        {
            var user = await access.GetUserAsync(principal, ct);
            await access.ClaimDemoIfUnownedAsync(user, ct);
            return await db.Projects.AsNoTracking().Where(x => (includeArchived == true || x.ArchivedAt == null) &&
                    db.ProjectMembers.Any(m => m.ProjectId == x.Id && m.UserId == user.Id))
                .OrderBy(x => x.Name).ToListAsync(ct);
        });
        api.MapGet("/projects/{projectId:guid}", async (Guid projectId, PlatformDbContext db, CancellationToken ct) =>
            await db.Projects.AsNoTracking().FirstOrDefaultAsync(x => x.Id == projectId, ct) is { } project
                ? Results.Ok(project) : Results.NotFound());
        api.MapPost("/projects", async (CreateProject request, ClaimsPrincipal principal, ProjectAccess access, PlatformDbContext db, CancellationToken ct) =>
        {
            if (!ValidName(request.Name)) return Bad("Name must be 2–120 characters.");
            if (request.Description?.Length > 2000) return Bad("Description is too long.");
            var slug = request.Slug is null ? Slugify(request.Name) : request.Slug.Trim();
            if (!ValidSlug(slug) || !Enum.IsDefined(request.Visibility)) return Bad("Slug must use 2–80 lowercase letters, numbers, or hyphens, and visibility must be valid.");
            if (await db.Projects.AnyAsync(x => x.Name == request.Name.Trim() || x.Slug == slug, ct)) return Conflict("Project name or slug already exists.");
            var user = await access.GetUserAsync(principal, ct);
            var project = new Project { Name = request.Name.Trim(), Description = request.Description?.Trim() ?? "", Slug = slug, Visibility = request.Visibility, CreatedByUserId = user.Id };
            db.Projects.Add(project);
            db.ProjectMembers.Add(new ProjectMember { ProjectId = project.Id, UserId = user.Id, Role = ProjectRole.Owner });
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/projects/{project.Id}", project);
        });
        api.MapPut("/projects/{projectId:guid}", async (Guid projectId, UpdateProject request, PlatformDbContext db, CancellationToken ct) =>
        {
            var project = await db.Projects.FindAsync([projectId], ct);
            if (project is null) return Results.NotFound();
            if (!ValidName(request.Name) || request.Description?.Length > 2000 || !ValidSlug(request.Slug) || !Enum.IsDefined(request.Visibility))
                return Bad("Invalid project fields.");
            if (await db.Projects.AnyAsync(x => x.Id != projectId && (x.Name == request.Name.Trim() || x.Slug == request.Slug), ct))
                return Conflict("Project name or slug already exists.");
            project.Name = request.Name.Trim();
            project.Description = request.Description?.Trim() ?? "";
            project.Slug = request.Slug;
            project.Visibility = request.Visibility;
            project.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.Ok(project);
        });
        api.MapPost("/projects/{projectId:guid}/archive", async (Guid projectId, PlatformDbContext db, CancellationToken ct) =>
        {
            var project = await db.Projects.FindAsync([projectId], ct);
            if (project is null) return Results.NotFound();
            project.ArchivedAt ??= DateTimeOffset.UtcNow;
            project.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.Ok(project);
        });
        api.MapGet("/projects/{projectId:guid}/members", async (Guid projectId, PlatformDbContext db, CancellationToken ct) =>
            await db.ProjectMembers.AsNoTracking().Where(x => x.ProjectId == projectId)
                .Join(db.Users, member => member.UserId, user => user.Id,
                    (member, user) => new { member.UserId, user.DisplayName, user.Email, member.Role }).ToListAsync(ct));
        api.MapPost("/projects/{projectId:guid}/members", async (Guid projectId, AddProjectMember request, PlatformDbContext db, CancellationToken ct) =>
        {
            if (!Enum.IsDefined(request.Role) || !await db.Users.AnyAsync(x => x.Id == request.UserId, ct)) return Bad("Invalid member.");
            if (await db.ProjectMembers.AnyAsync(x => x.ProjectId == projectId && x.UserId == request.UserId, ct)) return Conflict("User is already a member.");
            var member = new ProjectMember { ProjectId = projectId, UserId = request.UserId, Role = request.Role };
            db.ProjectMembers.Add(member);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/projects/{projectId}/members", member);
        });
        api.MapPost("/projects/{projectId:guid}/members/by-email", async (Guid projectId, AddProjectMemberByEmail request,
            ClaimsPrincipal principal, ProjectAccess access, PlatformDbContext db, CancellationToken ct) =>
        {
            if (!Enum.IsDefined(request.Role) || request.Email?.Trim().Length is not (>= 3 and <= 320) ||
                !request.Email.Contains('@')) return Bad("Invalid member email or role.");
            var owner = await access.GetUserAsync(principal, ct);
            var matches = await db.Users.Where(x => x.Issuer == owner.Issuer && x.Email == request.Email.Trim())
                .Take(2).ToListAsync(ct);
            if (matches.Count == 0) return Results.Problem("No signed-in user has that email for this identity provider.", statusCode: 404);
            if (matches.Count > 1) return Conflict("Email matches more than one signed-in user.");
            var user = matches[0];
            if (await db.ProjectMembers.AnyAsync(x => x.ProjectId == projectId && x.UserId == user.Id, ct))
                return Conflict("User is already a member.");
            var member = new ProjectMember { ProjectId = projectId, UserId = user.Id, Role = request.Role };
            db.ProjectMembers.Add(member);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/projects/{projectId}/members", member);
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
                x.TargetComponentId == request.TargetComponentId && x.Type == request.Type && x.Source == request.Source, ct))
                return Conflict("Dependency with this source already exists.");
            var dependency = new Dependency { ProjectId = projectId, SourceComponentId = request.SourceComponentId,
                TargetComponentId = request.TargetComponentId, Type = request.Type, Source = request.Source,
                LastObservedAt = request.Source == DependencySource.Observed ? DateTimeOffset.UtcNow : null };
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
        MapWorkItemEndpoints(api);
        MapOperationalIntegrationEndpoints(api);
        MapTopologyComparisonEndpoints(api);
    }

    private static bool ValidName(string? name) => name?.Trim().Length is >= 2 and <= 120;
    private static bool ValidSlug(string? slug) => slug is { Length: >= 2 and <= 80 } && Regex.IsMatch(slug, "^[a-z0-9]+(?:-[a-z0-9]+)*$");
    private static string Slugify(string name) => Regex.Replace(Regex.Replace(name.Trim().ToLowerInvariant(), "[^a-z0-9]+", "-"), "^-|-$", "") is { Length: >= 2 and <= 80 } slug ? slug : "project-" + Guid.NewGuid().ToString("N")[..8];
    private static IResult Bad(string detail) => Results.Problem(detail: detail, statusCode: 400);
    private static IResult Conflict(string detail) => Results.Problem(detail: detail, statusCode: 409);
    private static Task<bool> ValidEnvironment(PlatformDbContext db, Guid projectId, Guid environmentId, CancellationToken ct) =>
        db.Environments.AnyAsync(x => x.Id == environmentId && x.ProjectId == projectId, ct);
}
