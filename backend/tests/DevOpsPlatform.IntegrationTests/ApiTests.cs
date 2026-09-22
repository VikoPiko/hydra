using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using DevOpsPlatform.Persistence;

namespace DevOpsPlatform.IntegrationTests;

public class ApiTests
{
    [Fact]
    public async Task ProjectToTopologyRuntimeEventAndImpactFlow()
    {
        await using var factory = new TestFactory();
        using var client = factory.CreateClient();
        var project = await Post(client, "/api/projects", new { name = "API Test", description = "Test" });
        var projectId = project.GetProperty("id").GetGuid();
        var projects = await client.GetFromJsonAsync<JsonElement[]>("/api/projects");
        Assert.Contains(projects!, p => p.GetProperty("id").GetGuid() == projectId);

        var environment = await Post(client, $"/api/projects/{projectId}/environments", new { name = "Production", type = "Production" });
        var environmentId = environment.GetProperty("id").GetGuid();
        var source = await Post(client, $"/api/projects/{projectId}/components", new { name = "Order Service", type = "Service", description = "", repositoryUrl = (string?)null, owner = (string?)null });
        var target = await Post(client, $"/api/projects/{projectId}/components", new { name = "Payment Service", type = "Service", description = "", repositoryUrl = (string?)null, owner = (string?)null });
        var sourceId = source.GetProperty("id").GetGuid();
        var targetId = target.GetProperty("id").GetGuid();
        await Post(client, $"/api/projects/{projectId}/dependencies", new { sourceComponentId = sourceId, targetComponentId = targetId, type = "Http", source = "Declared" });

        var topology = await client.GetFromJsonAsync<JsonElement>($"/api/projects/{projectId}/environments/{environmentId}/topology");
        Assert.Equal(2, topology.GetProperty("nodes").GetArrayLength());
        Assert.Single(topology.GetProperty("edges").EnumerateArray());

        var runtimeResponse = await client.PutAsJsonAsync($"/api/projects/{projectId}/environments/{environmentId}/components/{targetId}/runtime",
            new { healthStatus = "Degraded", cpuPercent = 43.2, memoryPercent = 61, requestRate = 153.2, errorRatePercent = 7.4, latencyP95Ms = 410 });
        Assert.Equal(HttpStatusCode.OK, runtimeResponse.StatusCode);
        var updatedTopology = await client.GetFromJsonAsync<JsonElement>($"/api/projects/{projectId}/environments/{environmentId}/topology");
        Assert.Contains(updatedTopology.GetProperty("nodes").EnumerateArray(), n => n.GetProperty("id").GetGuid() == targetId && n.GetProperty("health").GetString() == "Degraded");
        var events = await client.GetFromJsonAsync<JsonElement[]>($"/api/projects/{projectId}/environments/{environmentId}/events");
        Assert.Contains(events!, e => e.GetProperty("type").GetString() == "HealthChanged");

        var impact = await client.GetFromJsonAsync<JsonElement>($"/api/projects/{projectId}/components/{targetId}/impact");
        Assert.Equal(sourceId, impact.GetProperty("directDependents")[0].GetProperty("id").GetGuid());
        Assert.Empty(impact.GetProperty("indirectDependents").EnumerateArray());
    }

    [Fact]
    public async Task DuplicateAndInvalidDependenciesReturnProblemDetails()
    {
        await using var factory = new TestFactory();
        using var client = factory.CreateClient();
        var project = await Post(client, "/api/projects", new { name = "Validation Test", description = "" });
        var projectId = project.GetProperty("id").GetGuid();
        var a = await Post(client, $"/api/projects/{projectId}/components", new { name = "Component A", type = "Service", description = "", repositoryUrl = (string?)null, owner = (string?)null });
        var b = await Post(client, $"/api/projects/{projectId}/components", new { name = "Component B", type = "Service", description = "", repositoryUrl = (string?)null, owner = (string?)null });
        var aId = a.GetProperty("id").GetGuid();
        var bId = b.GetProperty("id").GetGuid();
        var self = await client.PostAsJsonAsync($"/api/projects/{projectId}/dependencies", new { sourceComponentId = aId, targetComponentId = aId, type = "Http", source = "Declared" });
        Assert.Equal(HttpStatusCode.BadRequest, self.StatusCode);
        Assert.Equal("application/problem+json", self.Content.Headers.ContentType?.MediaType);
        var request = new { sourceComponentId = aId, targetComponentId = bId, type = "Http", source = "Declared" };
        await Post(client, $"/api/projects/{projectId}/dependencies", request);
        var duplicate = await client.PostAsJsonAsync($"/api/projects/{projectId}/dependencies", request);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task RuntimeChangeIsPublishedOnSseStream()
    {
        await using var factory = new TestFactory();
        using var client = factory.CreateClient();
        var project = await Post(client, "/api/projects", new { name = "Live Test", description = "" });
        var projectId = project.GetProperty("id").GetGuid();
        var environment = await Post(client, $"/api/projects/{projectId}/environments", new { name = "Production", type = "Production" });
        var environmentId = environment.GetProperty("id").GetGuid();
        var component = await Post(client, $"/api/projects/{projectId}/components", new { name = "Payment Service", type = "Service", description = "", repositoryUrl = (string?)null, owner = (string?)null });
        var componentId = component.GetProperty("id").GetGuid();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get,
            $"/api/projects/{projectId}/environments/{environmentId}/events/stream"),
            HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);
        await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
        using var reader = new StreamReader(stream);
        Assert.Equal(": connected", await reader.ReadLineAsync(timeout.Token));
        await reader.ReadLineAsync(timeout.Token);
        var update = await client.PutAsJsonAsync($"/api/projects/{projectId}/environments/{environmentId}/components/{componentId}/runtime",
            new { healthStatus = "Degraded", cpuPercent = 40, memoryPercent = 50, requestRate = 10, errorRatePercent = 4, latencyP95Ms = 200 }, timeout.Token);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal("event: runtime-state-updated", await reader.ReadLineAsync(timeout.Token));
        var data = await reader.ReadLineAsync(timeout.Token);
        Assert.Contains("\"healthStatus\":\"Degraded\"", data);
    }

    private static async Task<JsonElement> Post(HttpClient client, string path, object body)
    {
        var response = await client.PostAsJsonAsync(path, body);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>());
    }

    private sealed class TestFactory : WebApplicationFactory<Program>
    {
        private readonly string database = Guid.NewGuid().ToString();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureLogging(logging => { logging.ClearProviders(); logging.AddConsole(); });
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<PlatformDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<PlatformDbContext>>();
                services.RemoveAll<PlatformDbContext>();
                services.AddDbContext<PlatformDbContext>(options => options.UseInMemoryDatabase(database));
            });
        }
    }
}
