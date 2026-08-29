using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using SmartLinks.Management.Api.Authentication;
using SmartLinks.Management.Api.Endpoints;
using SmartLinks.Management.Api.ExceptionHandling;
using SmartLinks.Management.Application.SmartLinks.Create;
using SmartLinks.Management.Infrastructure.DependencyInjection;
using SmartLinks.Management.Application.SmartLinks.Queries;
using SmartLinks.Management.Application.SmartLinks.Update;
using SmartLinks.Management.Application.SmartLinks.Publication;
using SmartLinks.RuleEngine.DependencyInjection;

const string apiKeyAuthenticationScheme = "ApiKey";

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Management")
    ?? throw new InvalidOperationException("Не задана строка подключения Management");

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManagementApiExceptionHandler>();
builder.Services.AddHealthChecks();
builder.Services.AddManagementInfrastructure(connectionString);
builder.Services.AddSmartLinksRuleEngine();
builder.Services.AddScoped<CreateSmartLinkUseCase>();
builder.Services.AddScoped<GetSmartLinkUseCase>();
builder.Services.AddScoped<UpdateSmartLinkUseCase>();
builder.Services.AddScoped<PublishSmartLinkUseCase>();
builder.Services
    .AddAuthentication(apiKeyAuthenticationScheme)
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(apiKeyAuthenticationScheme, _ => { });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    // Liveness проверяет только работоспособность процесса и не зависит от PostgreSQL
    Predicate = _ => false
});

app.MapSmartLinkEndpoints();
app.MapPublishedConfigurationEndpoints();

app.Run();

// Открывает точку входа Management API для интеграционных тестов
public partial class Program
{
}