namespace DevOpsPlatform.Domain;

public enum RepositoryProvider { LocalDevelopment, GenericGit, AzureDevOps, GitHub, GitLab }
public enum RepositoryConnectionStatus { Connected, Degraded, AuthenticationFailed, Unavailable, Disconnected, Unknown }
public enum RepositoryScanStatus { Pending, Running, Succeeded, PartiallySucceeded, Failed }
public enum ComponentOrigin { Manual, Discovered, Imported, Observed }
public enum DiscoveryStatus { Active, Missing, Changed, Ignored }
public enum DiscoveryEvidenceType { Manual, DockerCompose, KubernetesManifest, OpenApi, PipelineConfiguration, Dockerfile, ProjectFile, PackageManifest, EnvironmentConfiguration, RuntimeObservation, Other }
public enum SynchronizationJobType { RepositoryScan, PipelineSync, DeploymentSync, ProviderRefresh }
public enum SynchronizationJobStatus { Queued, Running, Succeeded, PartiallySucceeded, Failed, Cancelled }

public sealed class RepositoryConnection
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public RepositoryProvider Provider { get; set; }
    public string Name { get; set; } = "";
    public string RepositoryUrl { get; set; } = "";
    public string DefaultBranch { get; set; } = "main";
    public string? ExternalRepositoryId { get; set; }
    public string AuthenticationMode { get; set; } = "None";
    public RepositoryConnectionStatus Status { get; set; } = RepositoryConnectionStatus.Unknown;
    public DateTimeOffset? LastSuccessfulSyncAt { get; set; }
    public DateTimeOffset? LastSyncAttemptAt { get; set; }
    public string? LastSyncError { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class RepositoryScan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RepositoryConnectionId { get; set; }
    public string? CommitSha { get; set; }
    public string Branch { get; set; } = "";
    public RepositoryScanStatus Status { get; set; } = RepositoryScanStatus.Pending;
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public int FilesScanned { get; set; }
    public int ComponentsDiscovered { get; set; }
    public int DependenciesDiscovered { get; set; }
    public int WarningsCount { get; set; }
    public string? ErrorMessage { get; set; }
    // Normalized candidates and warnings only. Repository file contents are never persisted here.
    public string? PreviewJson { get; set; }
    public DateTimeOffset? AppliedAt { get; set; }
}

public sealed class DependencyEvidence
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DependencyId { get; set; }
    public DiscoveryEvidenceType Type { get; set; }
    public DependencySource SourceKind { get; set; }
    public Guid? RepositoryConnectionId { get; set; }
    public Guid? ScanId { get; set; }
    public string? SourcePath { get; set; }
    public string? SourceLocation { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset? ObservedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class SynchronizationJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public Guid? RepositoryConnectionId { get; set; }
    public Guid? ScanId { get; set; }
    public SynchronizationJobType Type { get; set; }
    public SynchronizationJobStatus Status { get; set; } = SynchronizationJobStatus.Queued;
    public Guid? RequestedByUserId { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public int AttemptCount { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
