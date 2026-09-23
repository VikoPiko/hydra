using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
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
    public async Task ComponentLinksAndUnifiedActivityStayWithinProject()
    {
        await using var factory = new TestFactory();
        using var alice = factory.CreateAuthenticatedClient();
        using var bob = factory.CreateAuthenticatedClient("bob");
        var project = await Post(alice, "/api/projects", new { name = "Linked Work" });
        var other = await Post(alice, "/api/projects", new { name = "Other System" });
        var projectId = project.GetProperty("id").GetGuid();
        var otherId = other.GetProperty("id").GetGuid();
        var payment = await Post(alice, $"/api/projects/{projectId}/components", new { name = "Payment Service", type = "Service" });
        var unrelated = await Post(alice, $"/api/projects/{otherId}/components", new { name = "Billing Service", type = "Service" });
        var componentId = payment.GetProperty("id").GetGuid();
        var unrelatedId = unrelated.GetProperty("id").GetGuid();
        var item = await Post(alice, $"/api/projects/{projectId}/work-items", new { type = "Bug", title = "Payment retry fails", priority = "Critical" });
        var itemId = item.GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.BadRequest, (await alice.PostAsync($"/api/work-items/{itemId}/components/{unrelatedId}", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await bob.PostAsync($"/api/work-items/{itemId}/components/{componentId}", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await alice.PostAsync($"/api/work-items/{itemId}/components/{componentId}", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await alice.PostAsync($"/api/work-items/{itemId}/components/{componentId}", null)).StatusCode);
        var links = await alice.GetFromJsonAsync<JsonElement[]>($"/api/projects/{projectId}/work-item-component-links");
        Assert.Equal(componentId, Assert.Single(links!).GetProperty("componentId").GetGuid());
        Assert.Single((await alice.GetFromJsonAsync<JsonElement[]>($"/api/components/{componentId}/work-items"))!);

        var feed = await alice.GetFromJsonAsync<JsonElement[]>($"/api/projects/{projectId}/activity");
        Assert.Contains(feed!, entry => entry.GetProperty("kind").GetString() == "Operational" &&
            entry.GetProperty("summary").GetString()!.Contains("Payment Service"));
        Assert.Contains(feed!, entry => entry.GetProperty("kind").GetString() == "WorkItem" &&
            entry.GetProperty("summary").GetString()!.Contains("linked a component") &&
            entry.GetProperty("summary").GetString()!.Contains("Payment Service"));

        await alice.PatchAsJsonAsync($"/api/work-items/{itemId}/state", new { state = "Closed" });
        Assert.Empty((await alice.GetFromJsonAsync<JsonElement[]>($"/api/components/{componentId}/work-items"))!);
        Assert.Single((await alice.GetFromJsonAsync<JsonElement[]>($"/api/components/{componentId}/work-items?includeClosed=true"))!);
        Assert.Equal(HttpStatusCode.NoContent, (await alice.DeleteAsync($"/api/work-items/{itemId}/components/{componentId}")).StatusCode);
        Assert.Empty((await alice.GetFromJsonAsync<JsonElement[]>($"/api/projects/{projectId}/work-item-component-links"))!);
    }

    [Fact]
    public async Task WorkItemCrudFiltersStateAssignmentAndHistory()
    {
        await using var factory = new TestFactory();
        using var alice = factory.CreateAuthenticatedClient("alice");
        using var bob = factory.CreateAuthenticatedClient("bob");
        var project = await Post(alice, "/api/projects", new { name = "Board Project", slug = "board-project", visibility = "Private" });
        var projectId = project.GetProperty("id").GetGuid();
        var aliceId = (await alice.GetFromJsonAsync<JsonElement>("/api/me")).GetProperty("id").GetGuid();
        var first = await Post(alice, $"/api/projects/{projectId}/work-items", new {
            type = "Bug", title = "Payment fails", description = "On retry", priority = "High", assignedToUserId = aliceId,
            parentId = (Guid?)null, storyPoints = 3, originalEstimateHours = 4, remainingWorkHours = 4, iterationId = (Guid?)null, area = "Checkout"
        });
        var id = first.GetProperty("id").GetGuid();
        Assert.Equal("New", first.GetProperty("state").GetString());
        var second = await Post(alice, $"/api/projects/{projectId}/work-items", new {
            type = "Task", title = "Write tests", description = "", priority = "Medium", assignedToUserId = (Guid?)null,
            parentId = id, storyPoints = (int?)null, originalEstimateHours = (decimal?)null, remainingWorkHours = (decimal?)null,
            iterationId = (Guid?)null, area = (string?)null
        });
        Assert.Equal(id, second.GetProperty("parentId").GetGuid());
        Assert.Equal(HttpStatusCode.Forbidden, (await bob.GetAsync($"/api/work-items/{id}")).StatusCode);

        var state = await alice.PatchAsJsonAsync($"/api/work-items/{id}/state", new { state = "Active" });
        Assert.Equal(HttpStatusCode.OK, state.StatusCode);
        var filtered = await alice.GetFromJsonAsync<JsonElement[]>($"/api/projects/{projectId}/work-items?type=Bug&state=Active&assignedTo=me&search=payment");
        Assert.Single(filtered!);
        var badAssignee = await alice.PatchAsJsonAsync($"/api/work-items/{id}/assignment", new { assignedToUserId = Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.BadRequest, badAssignee.StatusCode);
        var unassign = await alice.PatchAsJsonAsync($"/api/work-items/{id}/assignment", new { assignedToUserId = (Guid?)null });
        Assert.Equal(HttpStatusCode.OK, unassign.StatusCode);
        var closed = await alice.PatchAsJsonAsync($"/api/work-items/{id}/state", new { state = "Closed" });
        Assert.Equal(HttpStatusCode.OK, closed.StatusCode);
        var detail = await alice.GetFromJsonAsync<JsonElement>($"/api/work-items/{id}");
        Assert.Equal("Closed", detail.GetProperty("state").GetString());
        Assert.NotEqual(JsonValueKind.Null, detail.GetProperty("closedAt").ValueKind);
        var activity = await alice.GetFromJsonAsync<JsonElement[]>($"/api/work-items/{id}/activity");
        Assert.Contains(activity!, entry => entry.GetProperty("type").GetString() == "StateChanged");
        Assert.Contains(activity!, entry => entry.GetProperty("type").GetString() == "Assigned");
        var tag = await Post(alice, $"/api/projects/{projectId}/tags", new { name = "urgent", colorToken = "red" });
        var tagId = tag.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await alice.PostAsync($"/api/work-items/{id}/tags/{tagId}", null)).StatusCode);
        var links = await alice.GetFromJsonAsync<JsonElement[]>($"/api/projects/{projectId}/work-item-tags");
        Assert.Contains(links!, link => link.GetProperty("workItemId").GetGuid() == id && link.GetProperty("tagId").GetGuid() == tagId);
        Assert.Equal(HttpStatusCode.NoContent, (await alice.DeleteAsync($"/api/work-items/{id}/tags/{tagId}")).StatusCode);
        Assert.Empty((await alice.GetFromJsonAsync<JsonElement[]>($"/api/projects/{projectId}/work-item-tags"))!);
    }

    [Fact]
    public async Task AuthenticationAndMembershipProtectProjectsAndComponents()
    {
        await using var factory = new TestFactory();
        using var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/projects")).StatusCode);

        using var alice = factory.CreateAuthenticatedClient("alice");
        using var bob = factory.CreateAuthenticatedClient("bob");
        var project = await Post(alice, "/api/projects", new { name = "Alice Project", slug = "alice-project", visibility = "Private" });
        var projectId = project.GetProperty("id").GetGuid();
        Assert.NotEqual(Guid.Empty, project.GetProperty("createdByUserId").GetGuid());
        Assert.Equal(HttpStatusCode.Forbidden, (await bob.GetAsync($"/api/projects/{projectId}")).StatusCode);
        Assert.Empty((await bob.GetFromJsonAsync<JsonElement[]>("/api/projects"))!);
        Assert.Equal(HttpStatusCode.Forbidden, (await bob.PostAsJsonAsync($"/api/projects/{projectId}/environments", new { name = "Production", type = "Production" })).StatusCode);

        var bobId = (await bob.GetFromJsonAsync<JsonElement>("/api/me")).GetProperty("id").GetGuid();
        await Post(alice, $"/api/projects/{projectId}/members", new { userId = bobId, role = "Viewer" });
        Assert.Equal(HttpStatusCode.OK, (await bob.GetAsync($"/api/projects/{projectId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await bob.PostAsJsonAsync($"/api/projects/{projectId}/environments", new { name = "Production", type = "Production" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await bob.PostAsync($"/api/projects/{projectId}/archive", null)).StatusCode);
    }

    [Fact]
    public async Task ProjectCreationUpdateAndArchivePersistAcrossRequests()
    {
        await using var factory = new TestFactory();
        using var client = factory.CreateAuthenticatedClient();
        var created = await Post(client, "/api/projects", new { name = "Real Project", description = "Original", slug = "real-project", visibility = "Private" });
        var id = created.GetProperty("id").GetGuid();
        Assert.Equal("real-project", created.GetProperty("slug").GetString());
        Assert.Equal("Private", created.GetProperty("visibility").GetString());
        Assert.Empty((await client.GetFromJsonAsync<JsonElement[]>($"/api/projects/{id}/components"))!);

        var duplicate = await client.PostAsJsonAsync("/api/projects", new { name = "Another Project", slug = "real-project", visibility = "Private" });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var invalid = await client.PostAsJsonAsync("/api/projects", new { name = "Another Project", slug = "Bad Slug", visibility = "Private" });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        var updated = await client.PutAsJsonAsync($"/api/projects/{id}", new { name = "Real Project", description = "Updated", slug = "real-project-v2", visibility = "Internal" });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var detail = await client.GetFromJsonAsync<JsonElement>($"/api/projects/{id}");
        Assert.Equal("Updated", detail.GetProperty("description").GetString());
        Assert.Equal("real-project-v2", detail.GetProperty("slug").GetString());

        var archived = await client.PostAsync($"/api/projects/{id}/archive", null);
        Assert.Equal(HttpStatusCode.OK, archived.StatusCode);
        Assert.DoesNotContain((await client.GetFromJsonAsync<JsonElement[]>("/api/projects"))!, p => p.GetProperty("id").GetGuid() == id);
        Assert.Contains((await client.GetFromJsonAsync<JsonElement[]>("/api/projects?includeArchived=true"))!, p => p.GetProperty("id").GetGuid() == id);
    }

    [Fact]
    public async Task ProjectToTopologyRuntimeEventAndImpactFlow()
    {
        await using var factory = new TestFactory();
        using var client = factory.CreateAuthenticatedClient();
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
        using var client = factory.CreateAuthenticatedClient();
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
        using var client = factory.CreateAuthenticatedClient();
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
        string? data = null;
        for (var i = 0; i < 10; i++)
        {
            if (await reader.ReadLineAsync(timeout.Token) != "event: runtime-state-updated") continue;
            data = await reader.ReadLineAsync(timeout.Token);
            break;
        }
        Assert.NotNull(data);
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

        public HttpClient CreateAuthenticatedClient(string subject = "alice")
        {
            var client = CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-Subject", subject);
            return client;
        }

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
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                    options.DefaultForbidScheme = "Test";
                }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
            });
        }
    }

    private sealed class TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-Test-Subject", out var subject) || string.IsNullOrWhiteSpace(subject))
                return Task.FromResult(AuthenticateResult.NoResult());
            var claims = new[] { new Claim("iss", "test-issuer"), new Claim("sub", subject.ToString()), new Claim("name", subject.ToString()) };
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name)), Scheme.Name)));
        }
    }
}
