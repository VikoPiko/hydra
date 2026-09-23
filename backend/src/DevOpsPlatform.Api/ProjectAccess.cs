using System.Security.Claims;
using DevOpsPlatform.Domain;
using DevOpsPlatform.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevOpsPlatform.Api;

public sealed class ProjectAccess(PlatformDbContext db)
{
    public async Task<UserProfile> GetUserAsync(ClaimsPrincipal principal, CancellationToken ct)
    {
        var issuer = principal.FindFirstValue("iss") ?? throw new InvalidOperationException("Authenticated token lacks issuer.");
        var subject = principal.FindFirstValue("sub") ?? throw new InvalidOperationException("Authenticated token lacks subject.");
        var user = await db.Users.FirstOrDefaultAsync(x => x.Issuer == issuer && x.ExternalSubject == subject, ct);
        if (user is null)
        {
            user = new UserProfile { Issuer = issuer, ExternalSubject = subject };
            db.Users.Add(user);
        }
        user.Email = principal.FindFirstValue("email");
        user.DisplayName = principal.FindFirstValue("name") ?? principal.FindFirstValue("preferred_username") ?? subject;
        user.LastSeenAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return user;
    }

    public async Task<ProjectRole?> RoleAsync(Guid projectId, UserProfile user, CancellationToken ct) =>
        await db.ProjectMembers.AsNoTracking().Where(x => x.ProjectId == projectId && x.UserId == user.Id)
            .Select(x => (ProjectRole?)x.Role).FirstOrDefaultAsync(ct);

    public static bool Allows(ProjectRole? actual, ProjectRole required) => actual is not null && actual.Value >= required;

    public async Task ClaimDemoIfUnownedAsync(UserProfile user, CancellationToken ct)
    {
        var demoId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var demo = await db.Projects.FindAsync([demoId], ct);
        if (demo is null || await db.ProjectMembers.AnyAsync(x => x.ProjectId == demoId, ct)) return;
        demo.CreatedByUserId = user.Id;
        db.ProjectMembers.Add(new ProjectMember { ProjectId = demoId, UserId = user.Id, Role = ProjectRole.Owner });
        await db.SaveChangesAsync(ct);
    }
}
