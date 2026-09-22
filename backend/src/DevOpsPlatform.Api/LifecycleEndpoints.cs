using System.Text.Json;
using DevOpsPlatform.Application;
using DevOpsPlatform.Domain;
using DevOpsPlatform.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevOpsPlatform.Api;

public static partial class PlatformEndpoints
{
    private static void MapLifecycleEndpoints(RouteGroupBuilder api, bool isDevelopment)
    {
        api.MapGet("/projects/{projectId:guid}/environments/{environmentId:guid}/deployments",
            async (Guid projectId, Guid environmentId, PlatformDbContext db, CancellationToken ct) =>
            {
                if (!await ValidEnvironment(db, projectId, environmentId, ct)) return Results.NotFound();
                return Results.Ok(await db.Deployments.AsNoTracking().Where(x => x.EnvironmentId == environmentId)
                    .OrderByDescending(x => x.StartedAt).Take(100).ToListAsync(ct));
            });
        api.MapGet("/components/{componentId:guid}/deployments", async (Guid componentId, PlatformDbContext db, CancellationToken ct) =>
        {
            if (!await db.Components.AnyAsync(x => x.Id == componentId, ct)) return Results.NotFound();
            return Results.Ok(await db.Deployments.AsNoTracking().Where(x => x.ComponentId == componentId)
                .OrderByDescending(x => x.StartedAt).Take(100).ToListAsync(ct));
        });
        api.MapPost("/projects/{projectId:guid}/deployments", async (Guid projectId, CreateDeployment request,
            PlatformDbContext db, ILiveEventBus bus, CancellationToken ct) =>
        {
            if (!await db.Projects.AnyAsync(x => x.Id == projectId, ct)) return Results.NotFound();
            if (!await ValidEnvironment(db, projectId, request.EnvironmentId, ct) ||
                !await db.Components.AnyAsync(x => x.Id == request.ComponentId && x.ProjectId == projectId, ct))
                return Bad("Environment and component must belong to the project.");
            var startedAt = (request.StartedAt ?? DateTimeOffset.UtcNow).ToUniversalTime();
            var completedAt = request.CompletedAt?.ToUniversalTime();
            if (string.IsNullOrWhiteSpace(request.Version) || request.Version.Length > 120 ||
                request.CommitSha?.Length > 120 || request.ArtifactReference?.Length > 500 || !Enum.IsDefined(request.Status) ||
                completedAt < startedAt) return Bad("Invalid deployment fields.");
            var deployment = new Deployment { ProjectId = projectId, EnvironmentId = request.EnvironmentId,
                ComponentId = request.ComponentId, Version = request.Version.Trim(), CommitSha = request.CommitSha,
                ArtifactReference = request.ArtifactReference, Status = request.Status,
                StartedAt = startedAt, CompletedAt = completedAt };
            db.Deployments.Add(deployment);
            var eventType = request.Status switch
            {
                DeploymentStatus.Succeeded => OperationalEventType.DeploymentCompleted,
                DeploymentStatus.Failed => OperationalEventType.DeploymentFailed,
                _ => OperationalEventType.DeploymentStarted
            };
            var evt = new OperationalEvent { ProjectId = projectId, EnvironmentId = request.EnvironmentId,
                ComponentId = request.ComponentId, Type = eventType,
                Severity = request.Status == DeploymentStatus.Failed ? EventSeverity.Error : EventSeverity.Info,
                Title = $"Deployment {request.Status}: {deployment.Version}" };
            db.OperationalEvents.Add(evt);
            await db.SaveChangesAsync(ct);
            bus.Publish(projectId, request.EnvironmentId, new LiveUpdate("deployment-updated", deployment));
            bus.Publish(projectId, request.EnvironmentId, new LiveUpdate("operational-event-created", evt));
            return Results.Created($"/api/components/{request.ComponentId}/deployments", deployment);
        });

        api.MapGet("/projects/{projectId:guid}/environments/{environmentId:guid}/runtime",
            async (Guid projectId, Guid environmentId, PlatformDbContext db, CancellationToken ct) =>
            {
                if (!await ValidEnvironment(db, projectId, environmentId, ct)) return Results.NotFound();
                return Results.Ok(await db.RuntimeStates.AsNoTracking().Where(x => x.EnvironmentId == environmentId).ToListAsync(ct));
            });
        api.MapPut("/projects/{projectId:guid}/environments/{environmentId:guid}/components/{componentId:guid}/runtime",
            async (Guid projectId, Guid environmentId, Guid componentId, UpdateRuntime request,
                PlatformDbContext db, ILiveEventBus bus, CancellationToken ct) =>
            {
                if (!await ValidEnvironment(db, projectId, environmentId, ct) ||
                    !await db.Components.AnyAsync(x => x.Id == componentId && x.ProjectId == projectId, ct)) return Results.NotFound();
                if (!Enum.IsDefined(request.HealthStatus) || !Percent(request.CpuPercent) || !Percent(request.MemoryPercent) ||
                    !Percent(request.ErrorRatePercent) || request.RequestRate < 0 || request.LatencyP95Ms < 0)
                    return Bad("Invalid runtime state fields.");
                var state = await db.RuntimeStates.FirstOrDefaultAsync(x => x.EnvironmentId == environmentId && x.ComponentId == componentId, ct);
                var previous = state?.HealthStatus ?? HealthStatus.Unknown;
                if (state is null)
                {
                    state = new RuntimeState { ProjectId = projectId, EnvironmentId = environmentId, ComponentId = componentId };
                    db.RuntimeStates.Add(state);
                }
                state.HealthStatus = request.HealthStatus;
                state.CpuPercent = request.CpuPercent;
                state.MemoryPercent = request.MemoryPercent;
                state.RequestRate = request.RequestRate;
                state.ErrorRatePercent = request.ErrorRatePercent;
                state.LatencyP95Ms = request.LatencyP95Ms;
                state.LastHeartbeatAt = DateTimeOffset.UtcNow;
                state.UpdatedAt = DateTimeOffset.UtcNow;
                OperationalEvent? evt = null;
                if (previous != state.HealthStatus)
                {
                    evt = new OperationalEvent { ProjectId = projectId, EnvironmentId = environmentId, ComponentId = componentId,
                        Type = OperationalEventType.HealthChanged,
                        Severity = state.HealthStatus switch { HealthStatus.Unhealthy => EventSeverity.Error,
                            HealthStatus.Degraded => EventSeverity.Warning, _ => EventSeverity.Info },
                        Title = $"Health changed from {previous} to {state.HealthStatus}" };
                    db.OperationalEvents.Add(evt);
                }
                await db.SaveChangesAsync(ct);
                bus.Publish(projectId, environmentId, new LiveUpdate("runtime-state-updated", state));
                if (evt is not null) bus.Publish(projectId, environmentId, new LiveUpdate("operational-event-created", evt));
                return Results.Ok(state);
            });

        api.MapGet("/projects/{projectId:guid}/environments/{environmentId:guid}/events",
            async (Guid projectId, Guid environmentId, int? limit, PlatformDbContext db, CancellationToken ct) =>
            {
                if (!await ValidEnvironment(db, projectId, environmentId, ct)) return Results.NotFound();
                return Results.Ok(await db.OperationalEvents.AsNoTracking()
                    .Where(x => x.ProjectId == projectId && x.EnvironmentId == environmentId)
                    .OrderByDescending(x => x.OccurredAt).Take(Math.Clamp(limit ?? 50, 1, 200)).ToListAsync(ct));
            });
        api.MapGet("/projects/{projectId:guid}/environments/{environmentId:guid}/events/stream",
            async (Guid projectId, Guid environmentId, HttpContext context, PlatformDbContext db, ILiveEventBus bus) =>
            {
                if (!await ValidEnvironment(db, projectId, environmentId, context.RequestAborted))
                { context.Response.StatusCode = 404; return; }
                context.Response.Headers.ContentType = "text/event-stream";
                context.Response.Headers.CacheControl = "no-cache";
                await context.Response.WriteAsync(": connected\n\n", context.RequestAborted);
                await context.Response.Body.FlushAsync(context.RequestAborted);
                try
                {
                    await foreach (var update in bus.SubscribeAsync(projectId, environmentId, context.RequestAborted))
                    {
                        var json = JsonSerializer.Serialize(update.Data, new JsonSerializerOptions(JsonSerializerDefaults.Web)
                        { Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } });
                        await context.Response.WriteAsync($"event: {update.EventName}\ndata: {json}\n\n", context.RequestAborted);
                        await context.Response.Body.FlushAsync(context.RequestAborted);
                    }
                }
                catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested) { }
            });

        if (isDevelopment)
        {
            api.MapPost("/dev/simulate/degradation/{componentId:guid}", (Guid componentId, Guid environmentId,
                PlatformDbContext db, ILiveEventBus bus, CancellationToken ct) => Simulate(componentId, environmentId, HealthStatus.Degraded, db, bus, ct));
            api.MapPost("/dev/simulate/recovery/{componentId:guid}", (Guid componentId, Guid environmentId,
                PlatformDbContext db, ILiveEventBus bus, CancellationToken ct) => Simulate(componentId, environmentId, HealthStatus.Healthy, db, bus, ct));
        }
    }

    private static bool Percent(double? value) => value is null || (double.IsFinite(value.Value) && value is >= 0 and <= 100);

    private static async Task<IResult> Simulate(Guid componentId, Guid environmentId, HealthStatus health,
        PlatformDbContext db, ILiveEventBus bus, CancellationToken ct)
    {
        var component = await db.Components.FirstOrDefaultAsync(x => x.Id == componentId, ct);
        if (component is null || !await ValidEnvironment(db, component.ProjectId, environmentId, ct)) return Results.NotFound();
        var state = await db.RuntimeStates.FirstOrDefaultAsync(x => x.ComponentId == componentId && x.EnvironmentId == environmentId, ct);
        var previous = state?.HealthStatus ?? HealthStatus.Unknown;
        if (state is null)
        {
            state = new RuntimeState { ProjectId = component.ProjectId, ComponentId = componentId, EnvironmentId = environmentId };
            db.RuntimeStates.Add(state);
        }
        state.HealthStatus = health;
        state.ErrorRatePercent = health == HealthStatus.Degraded ? 7.4 : 0.2;
        state.LatencyP95Ms = health == HealthStatus.Degraded ? 410 : 85;
        state.LastHeartbeatAt = DateTimeOffset.UtcNow;
        state.UpdatedAt = DateTimeOffset.UtcNow;
        OperationalEvent? evt = null;
        if (previous != health)
        {
            evt = new OperationalEvent { ProjectId = component.ProjectId, ComponentId = componentId,
                EnvironmentId = environmentId, Type = OperationalEventType.HealthChanged,
                Severity = health == HealthStatus.Degraded ? EventSeverity.Warning : EventSeverity.Info,
                Title = $"{component.Name} health changed from {previous} to {health}" };
            db.OperationalEvents.Add(evt);
        }
        await db.SaveChangesAsync(ct);
        bus.Publish(component.ProjectId, environmentId, new LiveUpdate("runtime-state-updated", state));
        if (evt is not null) bus.Publish(component.ProjectId, environmentId, new LiveUpdate("operational-event-created", evt));
        return Results.Ok(state);
    }
}
