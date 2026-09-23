using DevOpsPlatform.Domain;
using DevOpsPlatform.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevOpsPlatform.Api;

public static class DemoSeed
{
    public static async Task SeedAsync(PlatformDbContext db, CancellationToken ct = default)
    {
        var projectId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        if (!await db.Projects.AnyAsync(x => x.Id == projectId, ct))
        {
        var developmentId = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var productionId = Guid.Parse("20000000-0000-0000-0000-000000000002");
        var names = new[] { "Web Frontend", "API Gateway", "Order Service", "Payment Service",
            "Notification Worker", "Orders DB", "RabbitMQ", "Stripe" };
        var types = new[] { ComponentType.Frontend, ComponentType.Gateway, ComponentType.Service,
            ComponentType.Service, ComponentType.Worker, ComponentType.Database,
            ComponentType.MessageBroker, ComponentType.ExternalService };
        var ids = Enumerable.Range(1, names.Length).Select(i => Guid.Parse($"30000000-0000-0000-0000-{i:000000000000}")).ToArray();
        var at = new DateTimeOffset(2026, 9, 22, 10, 0, 0, TimeSpan.Zero);
        db.Projects.Add(new Project { Id = projectId, Name = "Commerce Demo", Slug = "commerce-demo", Visibility = ProjectVisibility.Internal, Description = "Distributed commerce system demo", CreatedAt = at, UpdatedAt = at });
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
        await SeedWorkAsync(db, projectId, ct);
    }

    private static async Task SeedWorkAsync(PlatformDbContext db, Guid projectId, CancellationToken ct)
    {
        var at = new DateTimeOffset(2026, 9, 22, 10, 0, 0, TimeSpan.Zero);
        var actor = await db.Users.FirstOrDefaultAsync(x => x.Issuer == "urn:devops:seed" && x.ExternalSubject == "commerce-demo", ct);
        if (actor is null)
        {
            actor = new UserProfile { Id = Guid.Parse("40000000-0000-0000-0000-000000000001"),
                Issuer = "urn:devops:seed", ExternalSubject = "commerce-demo", DisplayName = "Demo seed",
                CreatedAt = at, LastSeenAt = at };
            db.Users.Add(actor);
        }
        var examples = new (Guid Id, WorkItemType Type, string Title, WorkItemState State, WorkItemPriority Priority, int Minute)[]
        {
            (Guid.Parse("50000000-0000-0000-0000-000000000001"), WorkItemType.UserStory,
                "As a customer, I want to place an order", WorkItemState.Active, WorkItemPriority.High, -40),
            (Guid.Parse("50000000-0000-0000-0000-000000000002"), WorkItemType.Task,
                "Implement order validation", WorkItemState.Active, WorkItemPriority.Medium, -30),
            (Guid.Parse("50000000-0000-0000-0000-000000000003"), WorkItemType.Task,
                "Integrate Payment Service", WorkItemState.Closed, WorkItemPriority.Medium, -20),
            (Guid.Parse("50000000-0000-0000-0000-000000000004"), WorkItemType.Bug,
                "Payment failures after deployment", WorkItemState.Active, WorkItemPriority.Critical, 10)
        };
        var createdBug = false;
        foreach (var example in examples)
        {
            if (await db.WorkItems.AnyAsync(x => x.Id == example.Id, ct)) continue;
            if (example.Id == examples[3].Id) createdBug = true;
            var createdAt = at.AddMinutes(example.Minute);
            db.WorkItems.Add(new WorkItem { Id = example.Id, ProjectId = projectId, Type = example.Type,
                Title = example.Title, State = example.State, Priority = example.Priority,
                Description = example.Type == WorkItemType.Bug ? "Payment errors appeared after the recent deployment; investigate correlation." : "Commerce Demo planning example.",
                CreatedByUserId = actor.Id, CreatedAt = createdAt, UpdatedAt = createdAt,
                ClosedAt = example.State == WorkItemState.Closed ? createdAt.AddMinutes(5) : null });
            db.WorkItemActivities.Add(new WorkItemActivity { WorkItemId = example.Id, ActorUserId = actor.Id,
                Type = WorkItemActivityType.Created, NewValue = example.Title, OccurredAt = createdAt });
        }
        var tagDefinitions = new[] { ("backend", "blue"), ("frontend", "purple"), ("production", "amber"), ("payments", "red") };
        var tags = await db.Tags.Where(x => x.ProjectId == projectId).ToListAsync(ct);
        foreach (var (name, color) in tagDefinitions)
        {
            if (tags.Any(x => x.Name == name)) continue;
            var tag = new Tag { ProjectId = projectId, Name = name, ColorToken = color };
            db.Tags.Add(tag);
            tags.Add(tag);
        }
        var bugId = examples[3].Id;
        var paymentId = Guid.Parse("30000000-0000-0000-0000-000000000004");
        if (createdBug && await db.Components.AnyAsync(x => x.Id == paymentId && x.ProjectId == projectId, ct) &&
            !await db.WorkItemComponentLinks.AnyAsync(x => x.WorkItemId == bugId && x.ComponentId == paymentId, ct))
        {
            db.WorkItemComponentLinks.Add(new WorkItemComponentLink { WorkItemId = bugId, ComponentId = paymentId });
            db.WorkItemActivities.Add(new WorkItemActivity { WorkItemId = bugId, ActorUserId = actor.Id,
                Type = WorkItemActivityType.ComponentLinked, FieldName = "Component", NewValue = paymentId.ToString(),
                OccurredAt = at.AddMinutes(11) });
        }
        var paymentsTag = tags.Single(x => x.Name == "payments");
        if (createdBug && !await db.WorkItemTags.AnyAsync(x => x.WorkItemId == bugId && x.TagId == paymentsTag.Id, ct))
            db.WorkItemTags.Add(new WorkItemTag { WorkItemId = bugId, TagId = paymentsTag.Id });
        await db.SaveChangesAsync(ct);
    }
}
