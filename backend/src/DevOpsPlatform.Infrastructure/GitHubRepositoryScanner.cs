using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DevOpsPlatform.Application;
using DevOpsPlatform.Domain;

namespace DevOpsPlatform.Infrastructure;

// Uses GitHub's read-only API. No checkout, hooks, package restore, or repository code runs.
public sealed class GitHubRepositoryScanner(HttpClient http, IEnumerable<IDiscoveryParser> parsers) : IRepositoryScanner
{
    private readonly IDiscoveryParser[] parsers = parsers.ToArray();

    public static bool TryNormalize(string? url, out string normalized)
    {
        normalized = "";
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
            !string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase) || uri.Port != 443 ||
            uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0) return false;
        var parts = uri.AbsolutePath.Trim('/').Split('/');
        if (parts.Length != 2) return false;
        var owner = parts[0];
        var repo = parts[1].EndsWith(".git", StringComparison.OrdinalIgnoreCase) ? parts[1][..^4] : parts[1];
        if (!ValidSegment(owner) || !ValidSegment(repo)) return false;
        normalized = $"https://github.com/{owner}/{repo}";
        return true;
    }

    public static bool ValidBranch(string? branch) => branch is { Length: > 0 and <= 200 } &&
        !branch.StartsWith('/') && !branch.EndsWith('/') && !branch.Contains("..", StringComparison.Ordinal) &&
        Regex.IsMatch(branch, @"^[a-zA-Z0-9._/-]+$", RegexOptions.CultureInvariant);

    public async Task<RepositoryScanResult> ScanAsync(RepositoryConnection connection, CancellationToken cancellationToken)
    {
        if (connection.Provider != RepositoryProvider.GitHub ||
            !TryNormalize(connection.RepositoryUrl, out var url) || !ValidBranch(connection.DefaultBranch))
            throw new NotSupportedException("Invalid GitHub repository source.");
        var parts = new Uri(url).AbsolutePath.Trim('/').Split('/');
        var api = $"https://api.github.com/repos/{parts[0]}/{parts[1]}";
        using var commit = await GetJsonAsync($"{api}/commits/{Uri.EscapeDataString(connection.DefaultBranch)}", 1_000_000, cancellationToken);
        var sha = commit.RootElement.GetProperty("sha").GetString();
        var treeSha = commit.RootElement.GetProperty("commit").GetProperty("tree").GetProperty("sha").GetString();
        if (!ValidSha(sha) || !ValidSha(treeSha)) throw new InvalidDataException("GitHub returned invalid commit metadata.");
        using var tree = await GetJsonAsync($"{api}/git/trees/{treeSha}?recursive=1", 5_000_000, cancellationToken);
        var warnings = new List<DiscoveryWarning>();
        if (tree.RootElement.TryGetProperty("truncated", out var truncated) && truncated.GetBoolean())
            warnings.Add(new("repository", "GitHub truncated the repository tree; discovery is incomplete."));
        var components = new List<DiscoveredComponent>();
        var dependencies = new List<DiscoveredDependency>();
        var filesScanned = 0;
        var entries = tree.RootElement.GetProperty("tree");
        if (entries.GetArrayLength() > 20_000) throw new InvalidDataException("Repository tree limit exceeded.");
        foreach (var entry in entries.EnumerateArray())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (entry.GetProperty("type").GetString() != "blob" || entry.GetProperty("mode").GetString() != "100644") continue;
            var path = entry.GetProperty("path").GetString();
            if (path is null || path.StartsWith('/') || path.Contains('\\') ||
                path.Split('/').Any(x => x is "" or "." or "..")) continue;
            var parser = parsers.FirstOrDefault(x => x.CanParse(path));
            if (parser is null) continue;
            if (++filesScanned > 2_000) throw new InvalidDataException("Repository file limit exceeded.");
            if (entry.GetProperty("size").GetInt64() > 2_000_000)
            {
                warnings.Add(new(path, "File exceeds the 2 MB parsing limit."));
                continue;
            }
            var blobSha = entry.GetProperty("sha").GetString();
            if (!ValidSha(blobSha)) { warnings.Add(new(path, "Invalid blob identifier.")); continue; }
            using var blob = await GetJsonAsync($"{api}/git/blobs/{blobSha}", 3_000_000, cancellationToken);
            if (blob.RootElement.GetProperty("encoding").GetString() != "base64")
            {
                warnings.Add(new(path, "Unsupported file encoding."));
                continue;
            }
            var content = blob.RootElement.GetProperty("content").GetString();
            if (content is null) continue;
            var bytes = Convert.FromBase64String(content);
            if (bytes.Length > 2_000_000) { warnings.Add(new(path, "File exceeds the 2 MB parsing limit.")); continue; }
            var parsed = await parser.ParseAsync(new DiscoveredFile(path, Encoding.UTF8.GetString(bytes)), cancellationToken);
            components.AddRange(parsed.Components);
            dependencies.AddRange(parsed.Dependencies);
            warnings.AddRange(parsed.Warnings);
        }
        return new RepositoryScanResult(filesScanned, sha, new DiscoveryResult(components, dependencies, warnings));
    }

    private async Task<JsonDocument> GetJsonAsync(string url, int limit, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.ParseAdd("DevOpsPlatform-Discovery/1.0");
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (response.StatusCode == HttpStatusCode.NotFound) throw new InvalidDataException("Repository or branch was not found.");
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > limit) throw new InvalidDataException("GitHub response size limit exceeded.");
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var buffer = new MemoryStream();
        var chunk = new byte[16_384];
        int read;
        while ((read = await stream.ReadAsync(chunk, ct)) != 0)
        {
            if (buffer.Length + read > limit) throw new InvalidDataException("GitHub response size limit exceeded.");
            buffer.Write(chunk, 0, read);
        }
        buffer.Position = 0;
        return await JsonDocument.ParseAsync(buffer, cancellationToken: ct);
    }

    private static bool ValidSegment(string value) => value is { Length: > 0 and <= 100 } &&
        Regex.IsMatch(value, @"^[a-zA-Z0-9_.-]+$", RegexOptions.CultureInvariant) && value is not "." and not "..";
    private static bool ValidSha(string? value) => value is { Length: 40 } && value.All(Uri.IsHexDigit);
}

public sealed class RepositoryScanner(BundledRepositoryScanner bundled, GitHubRepositoryScanner github) : IRepositoryScanner
{
    public Task<RepositoryScanResult> ScanAsync(RepositoryConnection connection, CancellationToken cancellationToken) =>
        connection.Provider switch
        {
            RepositoryProvider.LocalDevelopment => bundled.ScanAsync(connection, cancellationToken),
            RepositoryProvider.GitHub => github.ScanAsync(connection, cancellationToken),
            _ => throw new NotSupportedException("Repository provider is not supported by this scanner.")
        };
}
