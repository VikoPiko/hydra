using System.Text.Json;
using System.Text.Json.Serialization;
using DevOpsPlatform.Application;
using DevOpsPlatform.Domain;
using DevOpsPlatform.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevOpsPlatform.Api;

internal static class DiscoveryJson
{
    public static readonly JsonSerializerOptions Options = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}

public sealed class RepositoryScanWorker(IServiceScopeFactory scopes, ILogger<RepositoryScanWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RecoverInterruptedJobsAsync(stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await ProcessNextAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception error)
            {
                logger.LogError("Repository scan worker cycle failed with {ErrorType}", error.GetType().Name);
            }
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    private async Task RecoverInterruptedJobsAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var jobs = await db.SynchronizationJobs.Where(x => x.Type == SynchronizationJobType.RepositoryScan &&
            x.Status == SynchronizationJobStatus.Running).ToListAsync(ct);
        var scans = await db.RepositoryScans.Where(x => x.Status == RepositoryScanStatus.Running).ToListAsync(ct);
        foreach (var job in jobs)
        {
            job.Status = SynchronizationJobStatus.Failed;
            job.LastError = "Scan worker stopped before completion.";
            job.CompletedAt = DateTimeOffset.UtcNow;
        }
        foreach (var scan in scans)
        {
            scan.Status = RepositoryScanStatus.Failed;
            scan.ErrorMessage = "Scan worker stopped before completion.";
            scan.CompletedAt = DateTimeOffset.UtcNow;
        }
        if (jobs.Count != 0 || scans.Count != 0) await db.SaveChangesAsync(ct);
    }

    private async Task ProcessNextAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var job = await db.SynchronizationJobs.OrderBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(x => x.Type == SynchronizationJobType.RepositoryScan && x.Status == SynchronizationJobStatus.Queued, ct);
        if (job is null || job.ScanId is null || job.RepositoryConnectionId is null) return;
        var scan = await db.RepositoryScans.FindAsync([job.ScanId.Value], ct);
        var connection = await db.RepositoryConnections.FindAsync([job.RepositoryConnectionId.Value], ct);
        if (scan is null || connection is null)
        {
            job.Status = SynchronizationJobStatus.Failed;
            job.LastError = "Repository scan or connection no longer exists.";
            job.CompletedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            return;
        }
        job.Status = SynchronizationJobStatus.Running;
        job.StartedAt = DateTimeOffset.UtcNow;
        job.AttemptCount++;
        scan.Status = RepositoryScanStatus.Running;
        scan.StartedAt = job.StartedAt.Value;
        connection.LastSyncAttemptAt = job.StartedAt;
        await db.SaveChangesAsync(ct);
        try
        {
            var result = await scope.ServiceProvider.GetRequiredService<IRepositoryScanner>().ScanAsync(connection, ct);
            scan.PreviewJson = JsonSerializer.Serialize(result.Discovery, DiscoveryJson.Options);
            scan.FilesScanned = result.FilesScanned;
            scan.ComponentsDiscovered = result.Discovery.Components.Count;
            scan.DependenciesDiscovered = result.Discovery.Dependencies.Count;
            scan.WarningsCount = result.Discovery.Warnings.Count;
            scan.CommitSha = result.CommitSha;
            scan.Status = result.Discovery.Warnings.Count == 0 ? RepositoryScanStatus.Succeeded : RepositoryScanStatus.PartiallySucceeded;
            job.Status = result.Discovery.Warnings.Count == 0 ? SynchronizationJobStatus.Succeeded : SynchronizationJobStatus.PartiallySucceeded;
            connection.Status = RepositoryConnectionStatus.Connected;
            connection.LastSuccessfulSyncAt = DateTimeOffset.UtcNow;
            connection.LastSyncError = null;
            logger.LogInformation("Repository scan {ScanId} completed with {Components} components and {Dependencies} dependencies",
                scan.Id, scan.ComponentsDiscovered, scan.DependenciesDiscovered);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception error)
        {
            scan.Status = RepositoryScanStatus.Failed;
            job.Status = SynchronizationJobStatus.Failed;
            scan.ErrorMessage = job.LastError = connection.LastSyncError = error is NotSupportedException
                ? "Repository provider is not supported by this scanner." : "Repository scan failed. Check source availability and retry.";
            connection.Status = RepositoryConnectionStatus.Degraded;
            logger.LogWarning("Repository scan {ScanId} failed with {ErrorType}", scan.Id, error.GetType().Name);
        }
        scan.CompletedAt = job.CompletedAt = DateTimeOffset.UtcNow;
        connection.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}
