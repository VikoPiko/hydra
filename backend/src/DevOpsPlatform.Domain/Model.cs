namespace DevOpsPlatform.Domain;

public enum EnvironmentType { Development, Testing, Staging, Production, Custom }
public enum ComponentType { Frontend, Service, Database, MessageBroker, Cache, ExternalService, Gateway, Worker, Other }
public enum DependencyType { Http, Database, Messaging, Cache, Infrastructure, External, Other }
public enum DependencySource { Declared, Discovered, Observed }
public enum DeploymentStatus { Pending, Running, Succeeded, Failed, RolledBack }
public enum HealthStatus { Healthy, Degraded, Unhealthy, Unknown }
public enum OperationalEventType { DeploymentStarted, DeploymentCompleted, DeploymentFailed, HealthChanged, MetricThresholdExceeded, ComponentRegistered, DependencyCreated, DependencyObserved, ConfigurationChanged, Other }
public enum EventSeverity { Info, Warning, Error, Critical }

public sealed class Project
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class SystemEnvironment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = "";
    public EnvironmentType Type { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class SystemComponent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public ComponentType Type { get; set; }
    public string? RepositoryUrl { get; set; }
    public string? Owner { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class Dependency
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public Guid SourceComponentId { get; set; }
    public Guid TargetComponentId { get; set; }
    public DependencyType Type { get; set; }
    public DependencySource Source { get; set; }
    public DateTimeOffset? LastObservedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public static bool IsValidPair(Guid source, Guid target) => source != Guid.Empty && target != Guid.Empty && source != target;
}

public sealed class Deployment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public Guid EnvironmentId { get; set; }
    public Guid ComponentId { get; set; }
    public string Version { get; set; } = "";
    public string? CommitSha { get; set; }
    public string? ArtifactReference { get; set; }
    public DeploymentStatus Status { get; set; }
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class RuntimeState
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public Guid EnvironmentId { get; set; }
    public Guid ComponentId { get; set; }
    public HealthStatus HealthStatus { get; set; } = HealthStatus.Unknown;
    public double? CpuPercent { get; set; }
    public double? MemoryPercent { get; set; }
    public double? RequestRate { get; set; }
    public double? ErrorRatePercent { get; set; }
    public double? LatencyP95Ms { get; set; }
    public DateTimeOffset? LastHeartbeatAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class OperationalEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public Guid? EnvironmentId { get; set; }
    public Guid? ComponentId { get; set; }
    public OperationalEventType Type { get; set; }
    public EventSeverity Severity { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
