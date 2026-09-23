using DevOpsPlatform.Application;
using DevOpsPlatform.Domain;
using DevOpsPlatform.Infrastructure;

namespace DevOpsPlatform.UnitTests;

public sealed class DockerComposeDiscoveryTests
{
    private readonly DockerComposeDiscoveryParser parser = new();

    [Fact]
    public async Task Discovers_services_types_and_mapping_dependencies()
    {
        var text = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Fixtures", "compose-basic.yml"));
        var result = await parser.ParseAsync(new DiscoveredFile("compose.yml", text), CancellationToken.None);

        Assert.Equal(5, result.Components.Count);
        Assert.Equal(ComponentType.Database, result.Components.Single(x => x.ExternalKey == "db").Type);
        Assert.Equal(ComponentType.MessageBroker, result.Components.Single(x => x.ExternalKey == "broker").Type);
        Assert.Equal(ComponentType.Cache, result.Components.Single(x => x.ExternalKey == "cache").Type);
        Assert.Equal(ComponentType.Service, result.Components.Single(x => x.ExternalKey == "api").Type);
        Assert.Equal(2, result.Dependencies.Count);
        Assert.Contains(result.Dependencies, x => x.SourceExternalKey == "api" && x.TargetExternalKey == "db" &&
            x.SourceLocation == "services.api.depends_on");
        Assert.Empty(result.Warnings);
    }

    [Theory]
    [InlineData("compose.yaml")]
    [InlineData("compose.yml")]
    [InlineData("docker-compose.yaml")]
    [InlineData("docker-compose.yml")]
    public void Recognizes_compose_file_names(string path) => Assert.True(parser.CanParse(path));

    [Fact]
    public async Task Invalid_yaml_and_aliases_become_visible_warnings()
    {
        var invalid = await parser.ParseAsync(new DiscoveredFile("compose.yaml", "services: ["), CancellationToken.None);
        var alias = await parser.ParseAsync(new DiscoveredFile("compose.yaml", "services: &s {api: {}}\nmore: *s"), CancellationToken.None);
        Assert.NotEmpty(invalid.Warnings);
        Assert.NotEmpty(alias.Warnings);
        Assert.Empty(alias.Components);
    }
}
