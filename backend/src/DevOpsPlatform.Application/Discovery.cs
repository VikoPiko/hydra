using DevOpsPlatform.Domain;

namespace DevOpsPlatform.Application;

public sealed record DiscoveredFile(string RelativePath, string Content);
public sealed record DiscoveredComponent(string ExternalKey, string Name, ComponentType Type,
    string SourcePath, string SourceLocation);
public sealed record DiscoveredDependency(string SourceExternalKey, string TargetExternalKey, DependencyType Type,
    string SourcePath, string SourceLocation, string Description);
public sealed record DiscoveryWarning(string SourcePath, string Message);
public sealed record DiscoveryResult(IReadOnlyList<DiscoveredComponent> Components,
    IReadOnlyList<DiscoveredDependency> Dependencies, IReadOnlyList<DiscoveryWarning> Warnings);
public sealed record RepositoryScanResult(int FilesScanned, string? CommitSha, DiscoveryResult Discovery);

public interface IDiscoveryParser
{
    bool CanParse(string relativePath);
    Task<DiscoveryResult> ParseAsync(DiscoveredFile file, CancellationToken cancellationToken);
}

public interface IRepositoryScanner
{
    Task<RepositoryScanResult> ScanAsync(RepositoryConnection connection, CancellationToken cancellationToken);
}
