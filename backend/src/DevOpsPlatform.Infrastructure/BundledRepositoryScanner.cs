using DevOpsPlatform.Application;
using DevOpsPlatform.Domain;

namespace DevOpsPlatform.Infrastructure;

// The first V3 source is a bundled, read-only repository. No repository content is executed.
public sealed class BundledRepositoryScanner(string rootPath, IEnumerable<IDiscoveryParser> parsers) : IRepositoryScanner
{
    private static readonly HashSet<string> IgnoredDirectories = new(StringComparer.OrdinalIgnoreCase)
        { ".git", "node_modules", "bin", "obj", "vendor", "dist", "build", ".next" };
    private readonly IDiscoveryParser[] parsers = parsers.ToArray();

    public async Task<RepositoryScanResult> ScanAsync(RepositoryConnection connection, CancellationToken cancellationToken)
    {
        if (connection.Provider != RepositoryProvider.LocalDevelopment || connection.RepositoryUrl != "demo://commerce-system")
            throw new NotSupportedException("This scanner supports only the bundled Commerce System repository.");
        var root = Path.GetFullPath(rootPath);
        if (!Directory.Exists(root)) throw new DirectoryNotFoundException("Bundled demo repository is unavailable.");
        var components = new List<DiscoveredComponent>();
        var dependencies = new List<DiscoveredDependency>();
        var warnings = new List<DiscoveryWarning>();
        var filesScanned = 0;
        foreach (var filePath in FilesUnder(root))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relative = Path.GetRelativePath(root, filePath).Replace('\\', '/');
            if (relative.StartsWith("../", StringComparison.Ordinal) || Path.IsPathRooted(relative))
                throw new InvalidDataException("Repository path escaped the configured root.");
            var parser = parsers.FirstOrDefault(x => x.CanParse(relative));
            if (parser is null) continue;
            if (++filesScanned > 2_000) throw new InvalidDataException("Repository file limit exceeded.");
            var info = new FileInfo(filePath);
            if (info.Length > 2_000_000)
            {
                warnings.Add(new(relative, "File exceeds the 2 MB parsing limit."));
                continue;
            }
            var result = await parser.ParseAsync(new DiscoveredFile(relative, await File.ReadAllTextAsync(filePath, cancellationToken)), cancellationToken);
            components.AddRange(result.Components);
            dependencies.AddRange(result.Dependencies);
            warnings.AddRange(result.Warnings);
        }
        return new RepositoryScanResult(filesScanned, null, new DiscoveryResult(components, dependencies, warnings));
    }

    private static IEnumerable<string> FilesUnder(string root)
    {
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.TryPop(out var directory))
        {
            foreach (var child in Directory.EnumerateDirectories(directory))
            {
                if (IgnoredDirectories.Contains(Path.GetFileName(child)) ||
                    File.GetAttributes(child).HasFlag(FileAttributes.ReparsePoint)) continue;
                pending.Push(child);
            }
            foreach (var file in Directory.EnumerateFiles(directory))
            {
                if (!File.GetAttributes(file).HasFlag(FileAttributes.ReparsePoint)) yield return file;
            }
        }
    }
}
