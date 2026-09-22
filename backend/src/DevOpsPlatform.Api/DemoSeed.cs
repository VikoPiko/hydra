using DevOpsPlatform.Domain;
using DevOpsPlatform.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevOpsPlatform.Api;

public static class DemoSeed
{
    public static async Task SeedAsync(PlatformDbContext db, CancellationToken ct = default)
    {
        var projectId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        if (await db.Projects.AnyAsync(x => x.Id == projectId, ct)) return;
        var developmentId = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var productionId = Guid.Parse("20000000-0000-0000-0000-000000000002");
        var names = new[] { "Web Frontend", "API Gateway", "Order Service", "Payment Service",
            "Notification Worker", "Orders DB", "RabbitMQ", "Stripe" };
        var types = new[] { ComponentType.Frontend, ComponentType.Gateway, ComponentType.Service,
            ComponentType.Service, ComponentType.Worker, ComponentType.Database,
            ComponentType.MessageBroker, ComponentType.ExternalService };
        var ids = Enumerable.Range(1, names.Length).Select(i => Guid.Parse($"30000000-0000-0000-0000-{i:000000000000}")).ToArray();
        var at = new DateTimeOffset(2026, 9, 22, 10, 0, 0, TimeSpan.Zero);
        db.Projects.Add(new Project { Id = projectId, Name = "Commerce Demo", Description = "Distributed commerce system demo", CreatedAt = at, UpdatedAt = at });
        db.Environments.AddRange(
            new SystemEnvironment { Id = developmentId, ProjectId = projectId, Name = "Development", Type = EnvironmentType.Development, CreatedAt = at, UpdatedAt = at },
            new SystemEnvironment { Id = productionId, ProjectId = projectId, Name = "Production", Type = EnvironmentType.Production, CreatedAt = at, UpdatedAt = at });
        for (var i = 0; i < names.Length; i++)
        {
            db.Components.Add(new SystemComponent { Id = ids[i], ProjectId = projectId, Name = names[i], Type = types[i],
                Description = $"Demo {names[i].ToLowerInvariant()} component", Owner = i < 4 ? "Commerce team" : "Platform team",
                CreatedAt = at, UpdatedAt = at });
            db.RuntimeStates.Add(new RuntimeState { ProjectId = projectId, EnvironmentId = productionId,
                ComponentId = ids[i], HealthStatus = i == 3 ? HealthStatus.Degraded : HealthStatus.Healthy,
                CpuPercent = i == 3 ? 43.2 : 18 + i * 3, MemoryPercent = i == 3 ? 61 : 30 + i * 4,
                RequestRate = i == 3 ? 153.2 : 100 + i * 12, ErrorRatePercent = i == 3 ? 7.4 : 0.2,
                LatencyP95Ms = i == 3 ? 410 : 70 + i * 12, LastHeartbeatAt = at.AddMinutes(5), UpdatedAt = at.AddMinutes(5) });
        }
        var edges = new (int Source, int Target, DependencyType Type)[]
        {
            (0, 1, DependencyType.Http), (1, 2, DependencyType.Http), (2, 3, DependencyType.Http),
            (2, 5, DependencyType.Database), (2, 6, DependencyType.Messaging),
            (4, 6, DependencyType.Messaging), (3, 7, DependencyType.External)
        };
        foreach (var (source, target, type) in edges)
            db.Dependencies.Add(new Dependency { ProjectId = projectId, SourceComponentId = ids[source],
                TargetComponentId = ids[target], Type = type, Source = DependencySource.Declared, CreatedAt = at });
        for (var i = 0; i < names.Length; i++)
            db.Deployments.Add(new Deployment { ProjectId = projectId, EnvironmentId = productionId,
                ComponentId = ids[i], Version = i == 3 ? "1.4.0" : "1.0.0", Status = DeploymentStatus.Succeeded,
                StartedAt = at.AddMinutes(i == 3 ? -3 : -60 - i), CompletedAt = at.AddMinutes(i == 3 ? 0 : -58 - i),
                CreatedAt = at.AddMinutes(i == 3 ? -3 : -60 - i) });
        db.OperationalEvents.AddRange(
            new OperationalEvent { ProjectId = projectId, EnvironmentId = productionId, ComponentId = ids[3],
                Type = OperationalEventType.DeploymentCompleted, Severity = EventSeverity.Info,
                Title = "Payment Service v1.4.0 deployment completed", OccurredAt = at, CreatedAt = at },
            new OperationalEvent { ProjectId = projectId, EnvironmentId = productionId, ComponentId = ids[3],
                Type = OperationalEventType.MetricThresholdExceeded, Severity = EventSeverity.Warning,
                Title = "Payment Service error rate exceeded threshold", OccurredAt = at.AddMinutes(4), CreatedAt = at.AddMinutes(4) },
            new OperationalEvent { ProjectId = projectId, EnvironmentId = productionId, ComponentId = ids[3],
                Type = OperationalEventType.HealthChanged, Severity = EventSeverity.Warning,
                Title = "Payment Service health changed to Degraded", OccurredAt = at.AddMinutes(5), CreatedAt = at.AddMinutes(5) });
        await db.SaveChangesAsync(ct);
    }
}
