namespace DevOpsPlatform.Domain;

public enum EnvironmentType { Development, Testing, Staging, Production, Custom }
public enum ComponentType { Frontend, Service, Database, MessageBroker, Cache, ExternalService, Gateway, Worker, Other }
public enum DependencyType { Http, Database, Messaging, Cache, Infrastructure, External, Other }
public enum DependencySource { Declared, Discovered, Observed }
public enum DeploymentStatus { Pending, Running, Succeeded, Failed, RolledBack }
public enum HealthStatus { Healthy, Degraded, Unhealthy, Unknown }
public enum OperationalEventType { DeploymentStarted, DeploymentCompleted, DeploymentFailed, HealthChanged, MetricThresholdExceeded, ComponentRegistered, DependencyCreated, DependencyObserved, ConfigurationChanged, Other }
public enum EventSeverity { Info, Warning, Error, Critical }
public enum ProjectVisibility { Private, Internal }
public enum ProjectRole { Viewer, Contributor, Owner }
public enum WorkItemType { UserStory, Task, Bug }
public enum WorkItemState { New, Active, Resolved, Closed, Removed }
public enum WorkItemPriority { Critical, High, Medium, Low }
public enum WorkItemActivityType { Created, Updated, StateChanged, Assigned, TagAdded, TagRemoved }

public sealed class WorkItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public long SequenceNumber { get; set; }
    public WorkItemType Type { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public WorkItemState State { get; set; } = WorkItemState.New;
    public WorkItemPriority Priority { get; set; } = WorkItemPriority.Medium;
    public Guid? AssignedToUserId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid? ParentId { get; set; }
    public int? StoryPoints { get; set; }
    public decimal? OriginalEstimateHours { get; set; }
    public decimal? RemainingWorkHours { get; set; }
    public Guid? IterationId { get; set; }
    public string? Area { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ClosedAt { get; set; }
}

public sealed class WorkItemActivity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkItemId { get; set; }
    public Guid? ActorUserId { get; set; }
    public WorkItemActivityType Type { get; set; }
    public string? FieldName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class Tag
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = "";
    public string? ColorToken { get; set; }
}

public sealed class WorkItemTag
{
    public Guid WorkItemId { get; set; }
    public Guid TagId { get; set; }
}

public enum IterationState { Planned, Current, Completed }

public sealed class Iteration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = "";
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public IterationState State { get; set; } = IterationState.Planned;
}

public sealed class UserProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Issuer { get; set; } = "";
    public string ExternalSubject { get; set; } = "";
    public string? Email { get; set; }
    public string DisplayName { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastSeenAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class ProjectMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }
    public ProjectRole Role { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class Project
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Slug { get; set; } = "";
    public ProjectVisibility Visibility { get; set; } = ProjectVisibility.Private;
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }
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
