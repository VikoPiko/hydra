using DevOpsPlatform.Application;
using DevOpsPlatform.Domain;

namespace DevOpsPlatform.UnitTests;

public class ImpactAnalysisTests
{
    private static readonly Guid Web = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid Gateway = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid Order = Guid.Parse("00000000-0000-0000-0000-000000000003");
    private static readonly Guid Payment = Guid.Parse("00000000-0000-0000-0000-000000000004");
    private static readonly Guid Worker = Guid.Parse("00000000-0000-0000-0000-000000000005");
    private static SystemComponent Component(Guid id, string name) => new() { Id = id, Name = name };
    private static Dependency Edge(Guid source, Guid target) => new() { SourceComponentId = source, TargetComponentId = target, Type = DependencyType.Http };
    private static readonly SystemComponent[] Components = [Component(Web, "Web"), Component(Gateway, "Gateway"),
        Component(Order, "Order"), Component(Payment, "Payment"), Component(Worker, "Worker")];

    [Fact]
    public void ChainReturnsDirectAndIndirectDependents()
    {
        var result = ImpactAnalysis.Analyze(Payment, Components, [Edge(Web, Gateway), Edge(Gateway, Order), Edge(Order, Payment)]);
        Assert.Equal([Order], result.DirectDependents.Select(x => x.Id));
        Assert.Equal([Gateway, Web], result.IndirectDependents.Select(x => x.Id));
        Assert.Equal(3, result.AffectedPaths.Count);
    }

    [Fact]
    public void BranchingAndDuplicateRoutesReturnEachDependentOnce()
    {
        var result = ImpactAnalysis.Analyze(Payment, Components,
            [Edge(Order, Payment), Edge(Worker, Payment), Edge(Web, Order), Edge(Web, Worker)]);
        Assert.Equal(2, result.DirectDependents.Count);
        Assert.Single(result.IndirectDependents);
        Assert.Equal(Web, result.IndirectDependents[0].Id);
    }

    [Fact]
    public void DisconnectedComponentsAreNotAffected()
    {
        var result = ImpactAnalysis.Analyze(Payment, Components, [Edge(Web, Gateway)]);
        Assert.Empty(result.DirectDependents);
        Assert.Empty(result.IndirectDependents);
    }

    [Fact]
    public void CycleTerminatesWithoutIncludingOrigin()
    {
        var result = ImpactAnalysis.Analyze(Payment, Components,
            [Edge(Order, Payment), Edge(Gateway, Order), Edge(Payment, Gateway)]);
        Assert.Equal([Order], result.DirectDependents.Select(x => x.Id));
        Assert.Equal([Gateway], result.IndirectDependents.Select(x => x.Id));
        Assert.DoesNotContain(result.AffectedPaths, x => x.ComponentIds.Last() == Payment);
    }

    [Fact]
    public void DependencyRejectsSelfReference() => Assert.False(Dependency.IsValidPair(Payment, Payment));
}
