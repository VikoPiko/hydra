using DevOpsPlatform.Domain;
using Microsoft.EntityFrameworkCore;

namespace DevOpsPlatform.Persistence;

public sealed class PlatformDbContext(DbContextOptions<PlatformDbContext> options) : DbContext(options)
{
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<UserProfile> Users => Set<UserProfile>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<WorkItem> WorkItems => Set<WorkItem>();
    public DbSet<WorkItemActivity> WorkItemActivities => Set<WorkItemActivity>();
    public DbSet<Iteration> Iterations => Set<Iteration>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<WorkItemTag> WorkItemTags => Set<WorkItemTag>();
    public DbSet<WorkItemComponentLink> WorkItemComponentLinks => Set<WorkItemComponentLink>();
    public DbSet<SystemEnvironment> Environments => Set<SystemEnvironment>();
    public DbSet<SystemComponent> Components => Set<SystemComponent>();
    public DbSet<Dependency> Dependencies => Set<Dependency>();
    public DbSet<Deployment> Deployments => Set<Deployment>();
    public DbSet<RuntimeState> RuntimeStates => Set<RuntimeState>();
    public DbSet<OperationalEvent> OperationalEvents => Set<OperationalEvent>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Project>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.Slug).HasMaxLength(80).IsRequired();
            e.Property(x => x.Visibility).HasConversion<string>().HasMaxLength(16);
            e.HasIndex(x => x.Name).IsUnique();
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasOne<UserProfile>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.SetNull);
        });
        model.Entity<UserProfile>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Issuer).HasMaxLength(500).IsRequired();
            e.Property(x => x.ExternalSubject).HasMaxLength(255).IsRequired();
            e.Property(x => x.Email).HasMaxLength(320);
            e.Property(x => x.DisplayName).HasMaxLength(120);
            e.HasIndex(x => new { x.Issuer, x.ExternalSubject }).IsUnique();
        });
        model.Entity<ProjectMember>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Role).HasConversion<string>().HasMaxLength(16);
            e.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<UserProfile>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.ProjectId, x.UserId }).IsUnique();
        });
        model.Entity<Iteration>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.Property(x => x.State).HasConversion<string>().HasMaxLength(16);
            e.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.ProjectId, x.Name }).IsUnique();
        });
        model.Entity<WorkItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.SequenceNumber).ValueGeneratedOnAdd().UseIdentityByDefaultColumn();
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(16);
            e.Property(x => x.Title).HasMaxLength(240).IsRequired();
            e.Property(x => x.Description).HasMaxLength(10000);
            e.Property(x => x.State).HasConversion<string>().HasMaxLength(16);
            e.Property(x => x.Priority).HasConversion<string>().HasMaxLength(16);
            e.Property(x => x.OriginalEstimateHours).HasPrecision(10, 2);
            e.Property(x => x.RemainingWorkHours).HasPrecision(10, 2);
            e.Property(x => x.Area).HasMaxLength(120);
            e.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<UserProfile>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<UserProfile>().WithMany().HasForeignKey(x => x.AssignedToUserId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne<WorkItem>().WithMany().HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Iteration>().WithMany().HasForeignKey(x => x.IterationId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => x.SequenceNumber).IsUnique();
            e.HasIndex(x => new { x.ProjectId, x.State });
            e.HasIndex(x => new { x.ProjectId, x.Type });
            e.HasIndex(x => x.AssignedToUserId);
            e.HasIndex(x => x.IterationId);
        });
        model.Entity<WorkItemActivity>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(24);
            e.Property(x => x.FieldName).HasMaxLength(80);
            e.Property(x => x.OldValue).HasMaxLength(500);
            e.Property(x => x.NewValue).HasMaxLength(500);
            e.HasOne<WorkItem>().WithMany().HasForeignKey(x => x.WorkItemId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<UserProfile>().WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => new { x.WorkItemId, x.OccurredAt });
        });
        model.Entity<Tag>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(60).IsRequired();
            e.Property(x => x.ColorToken).HasMaxLength(24);
            e.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.ProjectId, x.Name }).IsUnique();
        });
        model.Entity<WorkItemTag>(e =>
        {
            e.HasKey(x => new { x.WorkItemId, x.TagId });
            e.HasOne<WorkItem>().WithMany().HasForeignKey(x => x.WorkItemId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Tag>().WithMany().HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.TagId);
        });
        model.Entity<WorkItemComponentLink>(e =>
        {
            e.HasKey(x => new { x.WorkItemId, x.ComponentId });
            e.HasOne<WorkItem>().WithMany().HasForeignKey(x => x.WorkItemId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<SystemComponent>().WithMany().HasForeignKey(x => x.ComponentId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.ComponentId);
        });
        model.Entity<SystemEnvironment>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(32);
            e.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.ProjectId, x.Name }).IsUnique();
        });
        model.Entity<SystemComponent>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.RepositoryUrl).HasMaxLength(500);
            e.Property(x => x.Owner).HasMaxLength(120);
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(32);
            e.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.ProjectId, x.Name }).IsUnique();
        });
        model.Entity<Dependency>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(32);
            e.Property(x => x.Source).HasConversion<string>().HasMaxLength(32);
            e.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<SystemComponent>().WithMany().HasForeignKey(x => x.SourceComponentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<SystemComponent>().WithMany().HasForeignKey(x => x.TargetComponentId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.SourceComponentId, x.TargetComponentId, x.Type, x.Source }).IsUnique();
            e.HasIndex(x => x.TargetComponentId);
        });
        model.Entity<Deployment>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Version).HasMaxLength(120).IsRequired();
            e.Property(x => x.CommitSha).HasMaxLength(120);
            e.Property(x => x.ArtifactReference).HasMaxLength(500);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            e.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<SystemEnvironment>().WithMany().HasForeignKey(x => x.EnvironmentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<SystemComponent>().WithMany().HasForeignKey(x => x.ComponentId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.EnvironmentId, x.StartedAt });
            e.HasIndex(x => new { x.ComponentId, x.StartedAt });
        });
        model.Entity<RuntimeState>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.HealthStatus).HasConversion<string>().HasMaxLength(32);
            e.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<SystemEnvironment>().WithMany().HasForeignKey(x => x.EnvironmentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<SystemComponent>().WithMany().HasForeignKey(x => x.ComponentId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.EnvironmentId, x.ComponentId }).IsUnique();
        });
        model.Entity<OperationalEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(40);
            e.Property(x => x.Severity).HasConversion<string>().HasMaxLength(16);
            e.Property(x => x.Title).HasMaxLength(240).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000);
            e.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<SystemEnvironment>().WithMany().HasForeignKey(x => x.EnvironmentId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne<SystemComponent>().WithMany().HasForeignKey(x => x.ComponentId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => x.OccurredAt);
            e.HasIndex(x => new { x.EnvironmentId, x.OccurredAt });
        });
    }
}

public sealed class DesignTimeDbContextFactory : Microsoft.EntityFrameworkCore.Design.IDesignTimeDbContextFactory<PlatformDbContext>
{
    public PlatformDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? "Host=localhost;Port=5432;Database=devops_platform;Username=devops;Password=devops_local";
        return new PlatformDbContext(new DbContextOptionsBuilder<PlatformDbContext>().UseNpgsql(connection).Options);
    }
}
