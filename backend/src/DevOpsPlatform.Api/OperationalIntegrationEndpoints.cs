using System.Security.Claims;
using DevOpsPlatform.Domain;
using DevOpsPlatform.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevOpsPlatform.Api;

public sealed record ProjectActivityDto(Guid Id, DateTimeOffset OccurredAt, string Kind, string Summary,
    string? ActorName, Guid? ComponentId, Guid? WorkItemId, Guid? EnvironmentId);

public static partial class PlatformEndpoints
{
    private static void MapOperationalIntegrationEndpoints(RouteGroupBuilder api)
    {
        api.MapGet("/projects/{projectId:guid}/work-item-component-links", async (Guid projectId, PlatformDbContext db, CancellationToken ct) =>
            Results.Ok(await db.WorkItemComponentLinks.AsNoTracking()
                .Join(db.WorkItems.Where(x => x.ProjectId == projectId), link => link.WorkItemId, item => item.Id,
                    (link, _) => new { link.WorkItemId, link.ComponentId }).ToListAsync(ct)));

        api.MapGet("/components/{componentId:guid}/work-items", async (Guid componentId, bool? includeClosed, PlatformDbContext db, CancellationToken ct) =>
        {
            if (!await db.Components.AnyAsync(x => x.Id == componentId, ct)) return Results.NotFound();
            var query = db.WorkItemComponentLinks.AsNoTracking().Where(x => x.ComponentId == componentId)
                .Join(db.WorkItems.AsNoTracking(), link => link.WorkItemId, item => item.Id, (_, item) => item);
            if (includeClosed != true) query = query.Where(x => x.State != WorkItemState.Closed && x.State != WorkItemState.Removed);
            return Results.Ok(await query.OrderBy(x => x.SequenceNumber).Take(100).ToListAsync(ct));
        });

        api.MapPost("/work-items/{workItemId:guid}/components/{componentId:guid}", async (Guid workItemId, Guid componentId,
            ClaimsPrincipal principal, ProjectAccess access, PlatformDbContext db, CancellationToken ct) =>
        {
            var item = await db.WorkItems.FindAsync([workItemId], ct);
            if (item is null) return Results.NotFound();
            if (!await db.Components.AnyAsync(x => x.Id == componentId && x.ProjectId == item.ProjectId, ct))
                return Bad("Component must belong to the work item's project.");
            if (await db.WorkItemComponentLinks.AnyAsync(x => x.WorkItemId == workItemId && x.ComponentId == componentId, ct))
                return Results.NoContent();
            var user = await access.GetUserAsync(principal, ct);
            db.WorkItemComponentLinks.Add(new WorkItemComponentLink { WorkItemId = workItemId, ComponentId = componentId });
            db.WorkItemActivities.Add(new WorkItemActivity { WorkItemId = workItemId, ActorUserId = user.Id,
                Type = WorkItemActivityType.ComponentLinked, FieldName = "Component", NewValue = componentId.ToString() });
            item.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        api.MapDelete("/work-items/{workItemId:guid}/components/{componentId:guid}", async (Guid workItemId, Guid componentId,
            ClaimsPrincipal principal, ProjectAccess access, PlatformDbContext db, CancellationToken ct) =>
        {
            var link = await db.WorkItemComponentLinks.FindAsync([workItemId, componentId], ct);
            if (link is null) return Results.NoContent();
            var user = await access.GetUserAsync(principal, ct);
            db.WorkItemComponentLinks.Remove(link);
            db.WorkItemActivities.Add(new WorkItemActivity { WorkItemId = workItemId, ActorUserId = user.Id,
                Type = WorkItemActivityType.ComponentUnlinked, FieldName = "Component", OldValue = componentId.ToString() });
            var item = await db.WorkItems.FindAsync([workItemId], ct);
            if (item is not null) item.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        api.MapGet("/projects/{projectId:guid}/activity", async (Guid projectId, int? limit, PlatformDbContext db, CancellationToken ct) =>
        {
            if (limit is < 1 or > 100) return Bad("Limit must be 1–100.");
            if (!await db.Projects.AnyAsync(x => x.Id == projectId, ct)) return Results.NotFound();
            var count = limit ?? 50;
            var events = await db.OperationalEvents.AsNoTracking().Where(x => x.ProjectId == projectId)
                .OrderByDescending(x => x.OccurredAt).Take(count).ToListAsync(ct);
            var work = await db.WorkItemActivities.AsNoTracking()
                .Join(db.WorkItems.AsNoTracking().Where(x => x.ProjectId == projectId),
                    activity => activity.WorkItemId, item => item.Id,
                    (activity, item) => new { Activity = activity, item.Title, item.SequenceNumber })
                .OrderByDescending(x => x.Activity.OccurredAt).Take(count).ToListAsync(ct);
            var actorIds = work.Where(x => x.Activity.ActorUserId is not null)
                .Select(x => x.Activity.ActorUserId!.Value).Distinct().ToArray();
            var actors = await db.Users.AsNoTracking().Where(x => actorIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.DisplayName, ct);
            var componentIds = work.Where(x => x.Activity.Type is WorkItemActivityType.ComponentLinked or WorkItemActivityType.ComponentUnlinked)
                .Select(x => Guid.TryParse(x.Activity.NewValue ?? x.Activity.OldValue, out var id) ? id : Guid.Empty)
                .Where(x => x != Guid.Empty).Distinct().ToArray();
            var components = await db.Components.AsNoTracking().Where(x => componentIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Name, ct);
            var feed = events.Select(x => new ProjectActivityDto(x.Id, x.OccurredAt, "Operational", x.Title,
                    null, x.ComponentId, null, x.EnvironmentId))
                .Concat(work.Select(x =>
                {
                    var action = x.Activity.Type switch
                    {
                        WorkItemActivityType.Created => "created",
                        WorkItemActivityType.StateChanged => $"moved from {x.Activity.OldValue} to {x.Activity.NewValue}",
                        WorkItemActivityType.Assigned => "changed assignment on",
                        WorkItemActivityType.TagAdded => "tagged",
                        WorkItemActivityType.TagRemoved => "removed a tag from",
                        WorkItemActivityType.ComponentLinked => "linked a component to",
                        WorkItemActivityType.ComponentUnlinked => "unlinked a component from",
                        _ => "updated"
                    };
                    var componentName = Guid.TryParse(x.Activity.NewValue ?? x.Activity.OldValue, out var componentId)
                        && components.TryGetValue(componentId, out var name) ? $" ({name})" : "";
                    var summary = $"Work item #{x.SequenceNumber} {action}: {x.Title}{componentName}";
                    var actor = x.Activity.ActorUserId is { } userId && actors.TryGetValue(userId, out var displayName)
                        ? displayName : null;
                    return new ProjectActivityDto(x.Activity.Id, x.Activity.OccurredAt, "WorkItem", summary,
                        actor, null, x.Activity.WorkItemId, null);
                }))
                .OrderByDescending(x => x.OccurredAt).Take(count).ToList();
            return Results.Ok(feed);
        });
    }
}
