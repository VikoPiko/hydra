using DevOpsPlatform.Api;
using DevOpsPlatform.Application;
using DevOpsPlatform.Domain;
using DevOpsPlatform.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevOpsPlatform.IntegrationTests;

public class PersistenceTests
{
    [Fact]
    public async Task DemoSeedSupportsTopologyAndImpactQueries()
    {
        await using var db = NewDatabase();
        await DemoSeed.SeedAsync(db);
        Assert.Single(await db.Projects.ToListAsync());
        var project = await db.Projects.SingleAsync();
        var production = await db.Environments.SingleAsync(x => x.Type == EnvironmentType.Production);
        var components = await db.Components.Where(x => x.ProjectId == project.Id).ToListAsync();
        var dependencies = await db.Dependencies.Where(x => x.ProjectId == project.Id).ToListAsync();
        var states = await db.RuntimeStates.Where(x => x.EnvironmentId == production.Id).ToListAsync();
        Assert.Equal(8, components.Count);
        Assert.Equal(7, dependencies.Count);
        Assert.Equal(8, states.Count);
        var payment = components.Single(x => x.Name == "Payment Service");
        Assert.Equal(HealthStatus.Degraded, states.Single(x => x.ComponentId == payment.Id).HealthStatus);
        var impact = ImpactAnalysis.Analyze(payment.Id, components, dependencies);
        Assert.Equal(["Order Service"], impact.DirectDependents.Select(x => x.Name));
        Assert.Equal(["API Gateway", "Web Frontend"], impact.IndirectDependents.Select(x => x.Name));
        await DemoSeed.SeedAsync(db);
        Assert.Equal(8, await db.Components.CountAsync());
    }

    [Fact]
    public async Task ProjectComponentAndDependencyCanBePersisted()
    {
        await using var db = NewDatabase();
        var project = new Project { Name = "Test System" };
        var a = new SystemComponent { ProjectId = project.Id, Name = "A" };
        var b = new SystemComponent { ProjectId = project.Id, Name = "B" };
        db.AddRange(project, a, b, new Dependency { ProjectId = project.Id, SourceComponentId = a.Id,
            TargetComponentId = b.Id, Type = DependencyType.Http, Source = DependencySource.Declared });
        await db.SaveChangesAsync();
        Assert.Single(await db.Projects.ToListAsync());
        Assert.Equal(2, await db.Components.CountAsync());
        Assert.Single(await db.Dependencies.ToListAsync());
    }

    private static PlatformDbContext NewDatabase() => new(new DbContextOptionsBuilder<PlatformDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
