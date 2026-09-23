using DevOpsPlatform.Application;
using DevOpsPlatform.Domain;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;

namespace DevOpsPlatform.Infrastructure;

public sealed class DockerComposeDiscoveryParser : IDiscoveryParser
{
    private static readonly HashSet<string> FileNames = new(StringComparer.OrdinalIgnoreCase)
        { "compose.yaml", "compose.yml", "docker-compose.yaml", "docker-compose.yml" };

    public bool CanParse(string relativePath) => FileNames.Contains(Path.GetFileName(relativePath));

    public Task<DiscoveryResult> ParseAsync(DiscoveredFile file, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var components = new List<DiscoveredComponent>();
        var dependencies = new List<DiscoveredDependency>();
        var warnings = new List<DiscoveryWarning>();
        if (file.Content.Length > 2_000_000)
        {
            warnings.Add(new(file.RelativePath, "Compose file exceeds the 2 MB parsing limit."));
            return Task.FromResult(new DiscoveryResult(components, dependencies, warnings));
        }
        try
        {
            CheckYamlLimits(file.Content);
            var stream = new YamlStream();
            stream.Load(new StringReader(file.Content));
            if (stream.Documents.Count != 1 || stream.Documents[0].RootNode is not YamlMappingNode root ||
                Get(root, "services") is not YamlMappingNode services)
            {
                warnings.Add(new(file.RelativePath, "Compose file must contain one document with a services mapping."));
                return Task.FromResult(new DiscoveryResult(components, dependencies, warnings));
            }
            if (services.Children.Count > 500) throw new InvalidDataException("Compose service limit (500) exceeded.");
            foreach (var (keyNode, serviceNode) in services.Children)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var key = (keyNode as YamlScalarNode)?.Value;
                if (string.IsNullOrWhiteSpace(key) || key.Length > 120 || serviceNode is not YamlMappingNode service)
                {
                    warnings.Add(new(file.RelativePath, "Skipped a service with an invalid name or definition."));
                    continue;
                }
                var image = (Get(service, "image") as YamlScalarNode)?.Value ?? "";
                components.Add(new(key, Humanize(key), Classify(image), file.RelativePath, $"services.{key}"));
            }
            var known = components.Select(x => x.ExternalKey).ToHashSet(StringComparer.Ordinal);
            foreach (var (keyNode, serviceNode) in services.Children)
            {
                var source = (keyNode as YamlScalarNode)?.Value;
                if (source is null || !known.Contains(source) || serviceNode is not YamlMappingNode service) continue;
                var dependsOn = Get(service, "depends_on");
                IEnumerable<string?> targets = dependsOn switch
                {
                    YamlSequenceNode sequence => sequence.Children.Select(x => (x as YamlScalarNode)?.Value),
                    YamlMappingNode mapping => mapping.Children.Keys.Select(x => (x as YamlScalarNode)?.Value),
                    null => [],
                    _ => []
                };
                if (dependsOn is not null and not YamlSequenceNode and not YamlMappingNode)
                    warnings.Add(new(file.RelativePath, $"services.{source}.depends_on is not a list or mapping."));
                foreach (var target in targets)
                {
                    if (target is null || !known.Contains(target))
                    {
                        warnings.Add(new(file.RelativePath, $"services.{source}.depends_on references an unknown service."));
                        continue;
                    }
                    if (source == target)
                    {
                        warnings.Add(new(file.RelativePath, $"services.{source}.depends_on references itself."));
                        continue;
                    }
                    dependencies.Add(new(source, target, DependencyType.Infrastructure, file.RelativePath,
                        $"services.{source}.depends_on", "Declared by Docker Compose depends_on"));
                }
            }
        }
        catch (Exception error) when (error is YamlException or InvalidDataException or ArgumentException)
        {
            components.Clear();
            dependencies.Clear();
            var detail = error is InvalidDataException ? error.Message : "Invalid YAML structure.";
            warnings.Add(new(file.RelativePath, $"Compose parsing failed: {detail}"));
        }
        return Task.FromResult(new DiscoveryResult(components, dependencies, warnings));
    }

    private static void CheckYamlLimits(string content)
    {
        var parser = new Parser(new StringReader(content));
        var depth = 0;
        var events = 0;
        while (parser.MoveNext())
        {
            if (++events > 100_000) throw new InvalidDataException("YAML event limit exceeded.");
            if (parser.Current is AnchorAlias) throw new InvalidDataException("YAML aliases are unsupported for repository discovery.");
            if (parser.Current is MappingStart or SequenceStart && ++depth > 48)
                throw new InvalidDataException("YAML nesting limit exceeded.");
            if (parser.Current is MappingEnd or SequenceEnd) depth--;
        }
    }

    private static YamlNode? Get(YamlMappingNode mapping, string key) => mapping.Children
        .FirstOrDefault(x => (x.Key as YamlScalarNode)?.Value == key).Value;

    private static ComponentType Classify(string image)
    {
        var name = image.ToLowerInvariant();
        if (new[] { "postgres", "mysql", "mariadb", "mssql", "mongo" }.Any(name.Contains)) return ComponentType.Database;
        if (new[] { "rabbitmq", "kafka", "nats" }.Any(name.Contains)) return ComponentType.MessageBroker;
        if (new[] { "redis", "memcached" }.Any(name.Contains)) return ComponentType.Cache;
        return ComponentType.Service;
    }

    private static string Humanize(string key) => string.Join(' ', key.Split(['-', '_'], StringSplitOptions.RemoveEmptyEntries)
        .Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
}
