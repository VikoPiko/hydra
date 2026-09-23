using DevOpsPlatform.Domain;
using DevOpsPlatform.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevOpsPlatform.Api;

public sealed record DriftEdge(Guid SourceComponentId, string SourceName, Guid TargetComponentId,
    string TargetName, DependencyType Type, DateTimeOffset? LastObservedAt);
public sealed record DriftResponse(string Scope, IReadOnlyList<DriftEdge> Matching,
    IReadOnlyList<DriftEdge> DeclaredOnly, IReadOnlyList<DriftEdge> ObservedOnly,
    IReadOnlyList<DriftEdge> Discovered);
public sealed record ComponentComparison(Guid ComponentId, string Name, bool LeftHasData, bool RightHasData,
    string? LeftVersion, string? RightVersion, HealthStatus? LeftHealth, HealthStatus? RightHealth, bool Different);
public sealed record EnvironmentComparison(Guid LeftEnvironmentId, string LeftEnvironmentName,
    Guid RightEnvironmentId, string RightEnvironmentName, string ComponentPresenceMeaning,
    string DependencyScope, int DeclaredDependencyCount, IReadOnlyList<ComponentComparison> Components);

public static partial class PlatformEndpoints
{
    private static void MapTopologyComparisonEndpoints(RouteGroupBuilder api)
    {
        api.MapGet("/projects/{projectId:guid}/environments/{environmentId:guid}/architecture-drift",
            async (Guid projectId, Guid environmentId, PlatformDbContext db, CancellationToken ct) =>
            {
                if (!await ValidEnvironment(db, projectId, environmentId, ct)) return Results.NotFound();
                var names = await db.Components.AsNoTracking().Where(x => x.ProjectId == projectId)
                    .ToDictionaryAsync(x => x.Id, x => x.Name, ct);
                var dependencies = await db.Dependencies.AsNoTracking().Where(x => x.ProjectId == projectId).ToListAsync(ct);
                DriftEdge Describe(Dependency dependency) => new(dependency.SourceComponentId,
                    names.GetValueOrDefault(dependency.SourceComponentId) ?? "Unknown component",
                    dependency.TargetComponentId, names.GetValueOrDefault(dependency.TargetComponentId) ?? "Unknown component",
                    dependency.Type, dependency.LastObservedAt);
                var matching = new List<DriftEdge>();
                var declaredOnly = new List<DriftEdge>();
                var observedOnly = new List<DriftEdge>();
                var discovered = new List<DriftEdge>();
                foreach (var group in dependencies.GroupBy(x => (x.SourceComponentId, x.TargetComponentId, x.Type)))
                {
                    var declared = group.FirstOrDefault(x => x.Source == DependencySource.Declared);
                    var observed = group.FirstOrDefault(x => x.Source == DependencySource.Observed);
                    var discoveredEdge = group.FirstOrDefault(x => x.Source == DependencySource.Discovered);
                    if (declared is not null && observed is not null) matching.Add(Describe(observed));
                    else if (declared is not null) declaredOnly.Add(Describe(declared));
                    else if (observed is not null) observedOnly.Add(Describe(observed));
                    if (discoveredEdge is not null) discovered.Add(Describe(discoveredEdge));
                }
                static List<DriftEdge> Sort(List<DriftEdge> edges) => edges
                    .OrderBy(x => x.SourceName).ThenBy(x => x.TargetName).ThenBy(x => x.Type).ToList();
                return Results.Ok(new DriftResponse("Project", Sort(matching), Sort(declaredOnly), Sort(observedOnly), Sort(discovered)));
            });

        api.MapGet("/projects/{projectId:guid}/environment-comparison", async (Guid projectId,
            Guid leftEnvironmentId, Guid rightEnvironmentId, PlatformDbContext db, CancellationToken ct) =>
        {
            if (leftEnvironmentId == rightEnvironmentId || leftEnvironmentId == Guid.Empty || rightEnvironmentId == Guid.Empty)
                return Bad("Choose two different environments.");
            var environments = await db.Environments.AsNoTracking().Where(x => x.ProjectId == projectId &&
                (x.Id == leftEnvironmentId || x.Id == rightEnvironmentId)).ToListAsync(ct);
            if (environments.Count != 2) return Results.NotFound();
            var left = environments.Single(x => x.Id == leftEnvironmentId);
            var right = environments.Single(x => x.Id == rightEnvironmentId);
            var components = await db.Components.AsNoTracking().Where(x => x.ProjectId == projectId)
                .OrderBy(x => x.Name).ToListAsync(ct);
            var deployments = await db.Deployments.AsNoTracking().Where(x => x.ProjectId == projectId &&
                (x.EnvironmentId == leftEnvironmentId || x.EnvironmentId == rightEnvironmentId)).ToListAsync(ct);
            var states = await db.RuntimeStates.AsNoTracking().Where(x => x.ProjectId == projectId &&
                (x.EnvironmentId == leftEnvironmentId || x.EnvironmentId == rightEnvironmentId)).ToListAsync(ct);
            var latest = deployments.Where(x => x.Status == DeploymentStatus.Succeeded)
                .GroupBy(x => (x.EnvironmentId, x.ComponentId))
                .ToDictionary(x => x.Key, x => x.OrderByDescending(d => d.CompletedAt ?? d.StartedAt).First().Version);
            var health = states.ToDictionary(x => (x.EnvironmentId, x.ComponentId), x => x.HealthStatus);
            var recorded = deployments.Select(x => (x.EnvironmentId, x.ComponentId))
                .Concat(states.Select(x => (x.EnvironmentId, x.ComponentId))).ToHashSet();
            var rows = components.Select(component =>
            {
                latest.TryGetValue((leftEnvironmentId, component.Id), out var leftVersion);
                latest.TryGetValue((rightEnvironmentId, component.Id), out var rightVersion);
                var leftHealth = health.TryGetValue((leftEnvironmentId, component.Id), out var lh) ? (HealthStatus?)lh : null;
                var rightHealth = health.TryGetValue((rightEnvironmentId, component.Id), out var rh) ? (HealthStatus?)rh : null;
                var leftHasData = recorded.Contains((leftEnvironmentId, component.Id));
                var rightHasData = recorded.Contains((rightEnvironmentId, component.Id));
                return new ComponentComparison(component.Id, component.Name, leftHasData, rightHasData,
                    leftVersion, rightVersion, leftHealth, rightHealth,
                    leftHasData != rightHasData || leftVersion != rightVersion || leftHealth != rightHealth);
            }).ToList();
            var declaredCount = await db.Dependencies.CountAsync(x => x.ProjectId == projectId && x.Source == DependencySource.Declared, ct);
            return Results.Ok(new EnvironmentComparison(left.Id, left.Name, right.Id, right.Name,
                "Has deployment or runtime record", "Project", declaredCount, rows));
        });
    }
}
