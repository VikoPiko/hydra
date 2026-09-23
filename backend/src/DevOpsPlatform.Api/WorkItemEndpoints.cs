using System.Security.Claims;
using DevOpsPlatform.Domain;
using DevOpsPlatform.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevOpsPlatform.Api;

public sealed record SaveWorkItem(WorkItemType Type, string Title, string? Description, WorkItemPriority Priority,
    Guid? AssignedToUserId, Guid? ParentId, int? StoryPoints, decimal? OriginalEstimateHours,
    decimal? RemainingWorkHours, Guid? IterationId, string? Area);
public sealed record ChangeWorkItemState(WorkItemState State);
public sealed record ChangeWorkItemAssignment(Guid? AssignedToUserId);
public sealed record CreateIteration(string Name, DateOnly? StartDate, DateOnly? EndDate, IterationState State);
public sealed record CreateTag(string Name, string? ColorToken);

public static partial class PlatformEndpoints
{
    private static void MapWorkItemEndpoints(RouteGroupBuilder api)
    {
        api.MapGet("/projects/{projectId:guid}/work-items", async (Guid projectId, string? type, string? state,
            string? priority, string? assignedTo, string? iteration, string? search, int? limit,
            ClaimsPrincipal principal, ProjectAccess access, PlatformDbContext db, CancellationToken ct) =>
        {
            if (!await db.Projects.AnyAsync(x => x.Id == projectId, ct)) return Results.NotFound();
            var query = db.WorkItems.AsNoTracking().Where(x => x.ProjectId == projectId);
            if (type is not null)
            {
                if (!Enum.TryParse<WorkItemType>(type, true, out var parsed) || !Enum.IsDefined(parsed)) return Bad("Invalid type filter.");
                query = query.Where(x => x.Type == parsed);
            }
            if (state is not null)
            {
                if (!Enum.TryParse<WorkItemState>(state, true, out var parsed) || !Enum.IsDefined(parsed)) return Bad("Invalid state filter.");
                query = query.Where(x => x.State == parsed);
            }
            else query = query.Where(x => x.State != WorkItemState.Removed);
            if (priority is not null)
            {
                if (!Enum.TryParse<WorkItemPriority>(priority, true, out var parsed) || !Enum.IsDefined(parsed)) return Bad("Invalid priority filter.");
                query = query.Where(x => x.Priority == parsed);
            }
            if (assignedTo is not null)
            {
                Guid assignedId;
                if (assignedTo.Equals("me", StringComparison.OrdinalIgnoreCase)) assignedId = (await access.GetUserAsync(principal, ct)).Id;
                else if (!Guid.TryParse(assignedTo, out assignedId)) return Bad("Invalid assignee filter.");
                query = query.Where(x => x.AssignedToUserId == assignedId);
            }
            if (iteration is not null)
            {
                if (iteration.Equals("current", StringComparison.OrdinalIgnoreCase))
                {
                    var currentIds = await db.Iterations.Where(x => x.ProjectId == projectId && x.State == IterationState.Current).Select(x => x.Id).ToListAsync(ct);
                    query = query.Where(x => x.IterationId != null && currentIds.Contains(x.IterationId.Value));
                }
                else if (iteration.Equals("backlog", StringComparison.OrdinalIgnoreCase)) query = query.Where(x => x.IterationId == null);
                else if (Guid.TryParse(iteration, out var iterationId)) query = query.Where(x => x.IterationId == iterationId);
                else return Bad("Invalid iteration filter.");
            }
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLowerInvariant();
                if (term.Length > 120) return Bad("Search term is too long.");
                query = query.Where(x => x.Title.ToLower().Contains(term) || x.Description.ToLower().Contains(term));
            }
            if (limit is < 1 or > 500) return Bad("Limit must be 1–500.");
            return Results.Ok(await query.OrderBy(x => x.SequenceNumber).Take(limit ?? 500).ToListAsync(ct));
        });

        api.MapGet("/work-items/{workItemId:guid}", async (Guid workItemId, PlatformDbContext db, CancellationToken ct) =>
            await db.WorkItems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == workItemId, ct) is { } item ? Results.Ok(item) : Results.NotFound());

        api.MapGet("/work-items/{workItemId:guid}/activity", async (Guid workItemId, PlatformDbContext db, CancellationToken ct) =>
            Results.Ok(await db.WorkItemActivities.AsNoTracking().Where(x => x.WorkItemId == workItemId)
                .OrderBy(x => x.OccurredAt).ToListAsync(ct)));

        api.MapPost("/projects/{projectId:guid}/work-items", async (Guid projectId, SaveWorkItem request,
            ClaimsPrincipal principal, ProjectAccess access, PlatformDbContext db, CancellationToken ct) =>
        {
            if (!await db.Projects.AnyAsync(x => x.Id == projectId && x.ArchivedAt == null, ct)) return Results.NotFound();
            if (!ValidWorkItem(request) || !await ValidWorkItemReferences(db, projectId, request, null, ct)) return Bad("Invalid work item fields or references.");
            var user = await access.GetUserAsync(principal, ct);
            var item = new WorkItem { ProjectId = projectId, Type = request.Type, Title = request.Title.Trim(),
                Description = request.Description?.Trim() ?? "", Priority = request.Priority, AssignedToUserId = request.AssignedToUserId,
                CreatedByUserId = user.Id, ParentId = request.ParentId, StoryPoints = request.StoryPoints,
                OriginalEstimateHours = request.OriginalEstimateHours, RemainingWorkHours = request.RemainingWorkHours,
                IterationId = request.IterationId, Area = request.Area?.Trim() };
            db.WorkItems.Add(item);
            db.WorkItemActivities.Add(new WorkItemActivity { WorkItemId = item.Id, ActorUserId = user.Id,
                Type = WorkItemActivityType.Created, NewValue = item.Title });
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/work-items/{item.Id}", item);
        });

        api.MapPut("/work-items/{workItemId:guid}", async (Guid workItemId, SaveWorkItem request,
            ClaimsPrincipal principal, ProjectAccess access, PlatformDbContext db, CancellationToken ct) =>
        {
            var item = await db.WorkItems.FindAsync([workItemId], ct);
            if (item is null) return Results.NotFound();
            if (!ValidWorkItem(request) || !await ValidWorkItemReferences(db, item.ProjectId, request, item.Id, ct)) return Bad("Invalid work item fields or references.");
            var user = await access.GetUserAsync(principal, ct);
            if (item.AssignedToUserId != request.AssignedToUserId)
                db.WorkItemActivities.Add(new WorkItemActivity { WorkItemId = item.Id, ActorUserId = user.Id,
                    Type = WorkItemActivityType.Assigned, FieldName = "AssignedToUserId", OldValue = item.AssignedToUserId?.ToString(), NewValue = request.AssignedToUserId?.ToString() });
            item.Type = request.Type;
            item.Title = request.Title.Trim();
            item.Description = request.Description?.Trim() ?? "";
            item.Priority = request.Priority;
            item.AssignedToUserId = request.AssignedToUserId;
            item.ParentId = request.ParentId;
            item.StoryPoints = request.StoryPoints;
            item.OriginalEstimateHours = request.OriginalEstimateHours;
            item.RemainingWorkHours = request.RemainingWorkHours;
            item.IterationId = request.IterationId;
            item.Area = request.Area?.Trim();
            item.UpdatedAt = DateTimeOffset.UtcNow;
            db.WorkItemActivities.Add(new WorkItemActivity { WorkItemId = item.Id, ActorUserId = user.Id,
                Type = WorkItemActivityType.Updated, NewValue = item.Title });
            await db.SaveChangesAsync(ct);
            return Results.Ok(item);
        });

        api.MapPatch("/work-items/{workItemId:guid}/state", async (Guid workItemId, ChangeWorkItemState request,
            ClaimsPrincipal principal, ProjectAccess access, PlatformDbContext db, CancellationToken ct) =>
        {
            var item = await db.WorkItems.FindAsync([workItemId], ct);
            if (item is null) return Results.NotFound();
            if (!Enum.IsDefined(request.State)) return Bad("Invalid state.");
            if (item.State == request.State) return Results.Ok(item);
            var user = await access.GetUserAsync(principal, ct);
            var old = item.State;
            item.State = request.State;
            item.ClosedAt = request.State is WorkItemState.Closed or WorkItemState.Removed ? DateTimeOffset.UtcNow : null;
            item.UpdatedAt = DateTimeOffset.UtcNow;
            db.WorkItemActivities.Add(new WorkItemActivity { WorkItemId = item.Id, ActorUserId = user.Id,
                Type = WorkItemActivityType.StateChanged, FieldName = "State", OldValue = old.ToString(), NewValue = item.State.ToString() });
            await db.SaveChangesAsync(ct);
            return Results.Ok(item);
        });

        api.MapPatch("/work-items/{workItemId:guid}/assignment", async (Guid workItemId, ChangeWorkItemAssignment request,
            ClaimsPrincipal principal, ProjectAccess access, PlatformDbContext db, CancellationToken ct) =>
        {
            var item = await db.WorkItems.FindAsync([workItemId], ct);
            if (item is null) return Results.NotFound();
            if (request.AssignedToUserId is { } assignee && !await db.ProjectMembers.AnyAsync(x => x.ProjectId == item.ProjectId && x.UserId == assignee, ct))
                return Bad("Assignee must be a project member.");
            if (item.AssignedToUserId == request.AssignedToUserId) return Results.Ok(item);
            var user = await access.GetUserAsync(principal, ct);
            var old = item.AssignedToUserId;
            item.AssignedToUserId = request.AssignedToUserId;
            item.UpdatedAt = DateTimeOffset.UtcNow;
            db.WorkItemActivities.Add(new WorkItemActivity { WorkItemId = item.Id, ActorUserId = user.Id,
                Type = WorkItemActivityType.Assigned, FieldName = "AssignedToUserId", OldValue = old?.ToString(), NewValue = item.AssignedToUserId?.ToString() });
            await db.SaveChangesAsync(ct);
            return Results.Ok(item);
        });

        api.MapGet("/projects/{projectId:guid}/iterations", async (Guid projectId, PlatformDbContext db, CancellationToken ct) =>
            await db.Iterations.AsNoTracking().Where(x => x.ProjectId == projectId).OrderBy(x => x.Name).ToListAsync(ct));
        api.MapPost("/projects/{projectId:guid}/iterations", async (Guid projectId, CreateIteration request, PlatformDbContext db, CancellationToken ct) =>
        {
            if (!await db.Projects.AnyAsync(x => x.Id == projectId, ct)) return Results.NotFound();
            if (!ValidName(request.Name) || !Enum.IsDefined(request.State) || request.StartDate > request.EndDate) return Bad("Invalid iteration.");
            if (await db.Iterations.AnyAsync(x => x.ProjectId == projectId && x.Name == request.Name.Trim(), ct)) return Conflict("Iteration name already exists.");
            var iteration = new Iteration { ProjectId = projectId, Name = request.Name.Trim(), StartDate = request.StartDate, EndDate = request.EndDate, State = request.State };
            db.Iterations.Add(iteration);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/projects/{projectId}/iterations", iteration);
        });

        api.MapGet("/projects/{projectId:guid}/tags", async (Guid projectId, PlatformDbContext db, CancellationToken ct) =>
            await db.Tags.AsNoTracking().Where(x => x.ProjectId == projectId).OrderBy(x => x.Name).ToListAsync(ct));
        api.MapPost("/projects/{projectId:guid}/tags", async (Guid projectId, CreateTag request, PlatformDbContext db, CancellationToken ct) =>
        {
            if (!await db.Projects.AnyAsync(x => x.Id == projectId, ct)) return Results.NotFound();
            if (request.Name?.Trim().Length is not (>= 1 and <= 60) ||
                request.ColorToken is not null and not ("blue" or "green" or "amber" or "red" or "slate" or "purple"))
                return Bad("Invalid tag name or color token.");
            if (await db.Tags.AnyAsync(x => x.ProjectId == projectId && x.Name == request.Name.Trim(), ct)) return Conflict("Tag name already exists.");
            var tag = new Tag { ProjectId = projectId, Name = request.Name.Trim(), ColorToken = request.ColorToken };
            db.Tags.Add(tag);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/projects/{projectId}/tags", tag);
        });
        api.MapGet("/projects/{projectId:guid}/work-item-tags", async (Guid projectId, PlatformDbContext db, CancellationToken ct) =>
            await db.WorkItemTags.AsNoTracking().Join(db.WorkItems.Where(x => x.ProjectId == projectId),
                link => link.WorkItemId, item => item.Id, (link, _) => new { link.WorkItemId, link.TagId }).ToListAsync(ct));
        api.MapPost("/work-items/{workItemId:guid}/tags/{tagId:guid}", async (Guid workItemId, Guid tagId,
            ClaimsPrincipal principal, ProjectAccess access, PlatformDbContext db, CancellationToken ct) =>
        {
            var projectId = await db.WorkItems.Where(x => x.Id == workItemId).Select(x => x.ProjectId).FirstOrDefaultAsync(ct);
            if (projectId == Guid.Empty || !await db.Tags.AnyAsync(x => x.Id == tagId && x.ProjectId == projectId, ct)) return Bad("Tag must belong to the work item's project.");
            if (await db.WorkItemTags.AnyAsync(x => x.WorkItemId == workItemId && x.TagId == tagId, ct)) return Results.NoContent();
            var user = await access.GetUserAsync(principal, ct);
            db.WorkItemTags.Add(new WorkItemTag { WorkItemId = workItemId, TagId = tagId });
            db.WorkItemActivities.Add(new WorkItemActivity { WorkItemId = workItemId, ActorUserId = user.Id,
                Type = WorkItemActivityType.TagAdded, FieldName = "Tag", NewValue = tagId.ToString() });
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });
        api.MapDelete("/work-items/{workItemId:guid}/tags/{tagId:guid}", async (Guid workItemId, Guid tagId,
            ClaimsPrincipal principal, ProjectAccess access, PlatformDbContext db, CancellationToken ct) =>
        {
            var link = await db.WorkItemTags.FindAsync([workItemId, tagId], ct);
            if (link is null) return Results.NoContent();
            var user = await access.GetUserAsync(principal, ct);
            db.WorkItemTags.Remove(link);
            db.WorkItemActivities.Add(new WorkItemActivity { WorkItemId = workItemId, ActorUserId = user.Id,
                Type = WorkItemActivityType.TagRemoved, FieldName = "Tag", OldValue = tagId.ToString() });
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });
    }

    private static bool ValidWorkItem(SaveWorkItem request) =>
        Enum.IsDefined(request.Type) && Enum.IsDefined(request.Priority) &&
        request.Title?.Trim().Length is >= 2 and <= 240 && (request.Description is null || request.Description.Length <= 10000) &&
        (request.Area is null || request.Area.Length <= 120) && (request.StoryPoints is null or >= 0) &&
        (request.OriginalEstimateHours is null or >= 0 and <= 99999999) &&
        (request.RemainingWorkHours is null or >= 0 and <= 99999999);

    private static async Task<bool> ValidWorkItemReferences(PlatformDbContext db, Guid projectId, SaveWorkItem request, Guid? currentId, CancellationToken ct)
    {
        if (request.AssignedToUserId is { } assignee && !await db.ProjectMembers.AnyAsync(x => x.ProjectId == projectId && x.UserId == assignee, ct)) return false;
        if (request.IterationId is { } iteration && !await db.Iterations.AnyAsync(x => x.ProjectId == projectId && x.Id == iteration, ct)) return false;
        if (request.ParentId is not { } parentId) return true;
        if (parentId == currentId) return false;
        var visited = new HashSet<Guid>();
        while (parentId != Guid.Empty && visited.Add(parentId))
        {
            var parent = await db.WorkItems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == parentId && x.ProjectId == projectId, ct);
            if (parent is null || parent.Id == currentId) return false;
            parentId = parent.ParentId ?? Guid.Empty;
        }
        return parentId == Guid.Empty;
    }
}
