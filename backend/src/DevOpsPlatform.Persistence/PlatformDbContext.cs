using DevOpsPlatform.Domain;
using Microsoft.EntityFrameworkCore;

namespace DevOpsPlatform.Persistence;

public sealed class PlatformDbContext(DbContextOptions<PlatformDbContext> options) : DbContext(options)
{
    public DbSet<Project> Projects => Set<Project>();
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
            e.HasIndex(x => x.Name).IsUnique();
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
            e.HasIndex(x => new { x.SourceComponentId, x.TargetComponentId, x.Type }).IsUnique();
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
