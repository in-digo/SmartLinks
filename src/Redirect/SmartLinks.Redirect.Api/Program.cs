using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using SmartLinks.Redirect.Application.DependencyInjection;
using SmartLinks.Redirect.Infrastructure.DependencyInjection;
using SmartLinks.Redirect.Infrastructure.HealthChecks;
using SmartLinks.Redirect.Infrastructure.Synchronization;
using SmartLinks.Redirect.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

var managementApiBaseAddressValue = builder.Configuration["ManagementApi:BaseAddress"];

if (string.IsNullOrWhiteSpace(managementApiBaseAddressValue))
    throw new InvalidOperationException("Не задан базовый адрес Management API");

if (!Uri.TryCreate(managementApiBaseAddressValue, UriKind.Absolute, out var managementApiBaseAddress))
    throw new InvalidOperationException("Задан некорректный базовый адрес Management API");

builder.Services.Configure<ConfigurationSynchronizationOptions>(builder.Configuration.GetSection("ConfigurationSynchronization"));

builder.Services
    .AddHealthChecks()
    .AddCheck<ConfigurationSynchronizationHealthCheck>( "configuration-synchronization", tags: ["ready"]);

builder.Services.AddRedirectApplication();
builder.Services.AddRedirectInfrastructure(managementApiBaseAddress);

var app = builder.Build();

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