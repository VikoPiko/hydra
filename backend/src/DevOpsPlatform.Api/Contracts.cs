using DevOpsPlatform.Domain;

namespace DevOpsPlatform.Api;

public sealed record CreateProject(string Name, string? Description, string? Slug = null, ProjectVisibility Visibility = ProjectVisibility.Private);
public sealed record UpdateProject(string Name, string? Description, string Slug, ProjectVisibility Visibility);
public sealed record AddProjectMember(Guid UserId, ProjectRole Role);
public sealed record AddProjectMemberByEmail(string Email, ProjectRole Role);
public sealed record CreateEnvironment(string Name, EnvironmentType Type);
public sealed record CreateComponent(string Name, string? Description, ComponentType Type, string? RepositoryUrl, string? Owner);
public sealed record CreateDependency(Guid SourceComponentId, Guid TargetComponentId, DependencyType Type, DependencySource Source);
public sealed record CreateDeployment(Guid EnvironmentId, Guid ComponentId, string Version, string? CommitSha,
    string? ArtifactReference, DeploymentStatus Status, DateTimeOffset? StartedAt, DateTimeOffset? CompletedAt);
public sealed record UpdateRuntime(HealthStatus HealthStatus, double? CpuPercent, double? MemoryPercent,
    double? RequestRate, double? ErrorRatePercent, double? LatencyP95Ms);
public sealed record TopologyMetrics(double? CpuPercent, double? MemoryPercent, double? RequestRate,
    double? ErrorRatePercent, double? LatencyP95Ms);
public sealed record TopologyNode(Guid Id, string Name, ComponentType Type, HealthStatus Health, string? Version,
    string? Description, string? RepositoryUrl, string? Owner, TopologyMetrics Metrics,
    ComponentOrigin Origin, DiscoveryStatus DiscoveryStatus, Guid? RepositoryConnectionId, DateTimeOffset? LastDiscoveredAt);
public sealed record TopologyEvidence(DiscoveryEvidenceType Type, DependencySource SourceKind, string? SourcePath,
    string? SourceLocation, string? Description, DateTimeOffset? ObservedAt, DateTimeOffset CreatedAt);
public sealed record TopologyEdge(Guid Id, Guid Source, Guid Target, DependencyType Type, DependencySource SourceKind,
    IReadOnlyList<TopologyEvidence> Evidence);
public sealed record TopologyResponse(Guid ProjectId, Guid EnvironmentId, DateTimeOffset GeneratedAt,
    IReadOnlyList<TopologyNode> Nodes, IReadOnlyList<TopologyEdge> Edges);
