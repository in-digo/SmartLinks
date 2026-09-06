using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using SmartLinks.Redirect.Application.DependencyInjection;
using SmartLinks.Redirect.Infrastructure.DependencyInjection;
using SmartLinks.Redirect.Infrastructure.HealthChecks;
using SmartLinks.Redirect.Infrastructure.Synchronization;
using SmartLinks.Redirect.Api.Middleware;
using SmartLinks.RuleEngine.Resolution;

var builder = WebApplication.CreateBuilder(args);

var managementApiBaseAddressValue = builder.Configuration["ManagementApi:BaseAddress"];
var geoIpDatabasePath = builder.Configuration["GeoIp:DatabasePath"];

if (string.IsNullOrWhiteSpace(managementApiBaseAddressValue))
    throw new InvalidOperationException("Не задан базовый адрес Management API");

if (!Uri.TryCreate(managementApiBaseAddressValue, UriKind.Absolute, out var managementApiBaseAddress))
    throw new InvalidOperationException("Задан некорректный базовый адрес Management API");

builder.Services.Configure<ConfigurationSynchronizationOptions>(builder.Configuration.GetSection("ConfigurationSynchronization"));

builder.Services
    .AddHealthChecks()
    .AddCheck<ConfigurationSynchronizationHealthCheck>("configuration-synchronization", tags: ["ready"]);

builder.Services.AddRedirectApplication();
builder.Services.AddRedirectInfrastructure(managementApiBaseAddress);
if (!string.IsNullOrWhiteSpace(geoIpDatabasePath))
    builder.Services.AddMaxMindGeoIp(geoIpDatabasePath);

var knownProxyNetworkValues = builder.Configuration
    .GetSection("ForwardedHeaders:KnownNetworks")
    .Get<string[]>() ?? [];
var knownProxyNetworks = new List<IPNetwork>();

foreach (var knownProxyNetworkValue in knownProxyNetworkValues)
{
    if (!System.Net.IPNetwork.TryParse(knownProxyNetworkValue, out var parsedKnownProxyNetwork))
        throw new InvalidOperationException($"Задана некорректная доверенная сеть reverse proxy: {knownProxyNetworkValue}");

    knownProxyNetworks.Add(new IPNetwork(parsedKnownProxyNetwork.BaseAddress, parsedKnownProxyNetwork.PrefixLength));
}

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
    options.ForwardLimit = 1;

    foreach (var knownProxyNetwork in knownProxyNetworks)
        options.KnownNetworks.Add(knownProxyNetwork);
});

var app = builder.Build();

// Проверяем доступность настроенной GeoIP-базы до начала обработки запросов
if (!string.IsNullOrWhiteSpace(geoIpDatabasePath))
    _ = app.Services.GetRequiredService<IClientLocationResolver>();

app.UseForwardedHeaders();
app.UseMiddleware<SmartLinkRedirectMiddleware>();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    // Liveness проверяет только работоспособность процесса
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    // Readiness зависит от завершения первоначальной синхронизации
    Predicate = healthCheck => healthCheck.Tags.Contains("ready")
});

app.Run();

// Открывает точку входа Redirect API для интеграционных тестов
public partial class Program
{
}