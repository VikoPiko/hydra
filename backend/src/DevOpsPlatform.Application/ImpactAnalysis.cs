using DevOpsPlatform.Domain;

namespace DevOpsPlatform.Application;

public sealed record ImpactComponent(Guid Id, string Name, int Distance);
public sealed record ImpactPath(IReadOnlyList<Guid> ComponentIds);
public sealed record ImpactResult(Guid ComponentId, IReadOnlyList<ImpactComponent> DirectDependents,
    IReadOnlyList<ImpactComponent> IndirectDependents, IReadOnlyList<ImpactPath> AffectedPaths);

public static class ImpactAnalysis
{
    // An edge A -> B means A depends on B. Traverse backwards from B.
    public static ImpactResult Analyze(Guid componentId, IEnumerable<SystemComponent> components, IEnumerable<Dependency> dependencies)
    {
        var names = components.ToDictionary(x => x.Id, x => x.Name);
        var incoming = dependencies.GroupBy(x => x.TargetComponentId)
            .ToDictionary(x => x.Key, x => x.Select(d => d.SourceComponentId).Distinct().Order().ToArray());
        var visited = new HashSet<Guid> { componentId };
        var queue = new Queue<(Guid Id, List<Guid> Path)>();
        queue.Enqueue((componentId, new List<Guid> { componentId }));
        var found = new List<ImpactComponent>();
        var paths = new List<ImpactPath>();
        while (queue.TryDequeue(out var current))
        {
            if (!incoming.TryGetValue(current.Id, out var dependents)) continue;
            foreach (var dependent in dependents)
            {
                if (!visited.Add(dependent)) continue;
                var path = new List<Guid>(current.Path) { dependent };
                if (names.TryGetValue(dependent, out var name))
                {
                    found.Add(new ImpactComponent(dependent, name, path.Count - 1));
                    paths.Add(new ImpactPath(path));
                    queue.Enqueue((dependent, path));
                }
            }
        }
        return new ImpactResult(componentId, found.Where(x => x.Distance == 1).ToArray(),
            found.Where(x => x.Distance > 1).ToArray(), paths);
    }
}
