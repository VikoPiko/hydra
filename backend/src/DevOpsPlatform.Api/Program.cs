using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using DevOpsPlatform.Api;
using DevOpsPlatform.Application;
using DevOpsPlatform.Infrastructure;
using DevOpsPlatform.Persistence;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();
var connection = builder.Configuration.GetConnectionString("Postgres")
    ?? "Host=localhost;Port=5432;Database=devops_platform;Username=devops;Password=devops_local";
builder.Services.AddDbContext<PlatformDbContext>(options => options.UseNpgsql(connection));
builder.Services.AddScoped<ProjectAccess>();
var issuer = builder.Configuration["Oidc:Issuer"] ?? "http://localhost:8080/realms/devops";
var audience = builder.Configuration["Oidc:Audience"] ?? "devops-api";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.Authority = issuer;
    options.MetadataAddress = builder.Configuration["Oidc:MetadataAddress"] ?? $"{issuer}/.well-known/openid-configuration";
    options.Audience = audience;
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true
    };
});
builder.Services.AddAuthorization();
builder.Services.AddSingleton<ILiveEventBus, InMemoryLiveEventBus>();
builder.Services.AddSingleton<IDiscoveryParser, DockerComposeDiscoveryParser>();
builder.Services.AddSingleton<IRepositoryScanner>(provider => new BundledRepositoryScanner(
    Path.Combine(AppContext.BaseDirectory, "examples", "commerce-system"), provider.GetServices<IDiscoveryParser>()));
builder.Services.AddHostedService<RepositoryScanWorker>();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.WithOrigins(
    builder.Configuration["FrontendOrigin"] ?? "http://localhost:3000").AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddHealthChecks().AddCheck<PostgresHealthCheck>("postgres");

var app = builder.Build();
app.UseExceptionHandler();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.MapHealthChecks("/health/ready");
app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapGet("/health", () => Results.Ok(new { status = "live" }));
app.MapPlatformEndpoints();

if (app.Environment.IsDevelopment() && app.Configuration.GetValue("SeedDemo", true))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
    await db.Database.MigrateAsync();
    await DemoSeed.SeedAsync(db);
}

app.Run();

public partial class Program;

sealed class PostgresHealthCheck(PlatformDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        await db.Database.CanConnectAsync(cancellationToken) ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy();
}

sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled API error");
        context.Response.StatusCode = 500;
        return new ValueTask<bool>(false);
    }
}
