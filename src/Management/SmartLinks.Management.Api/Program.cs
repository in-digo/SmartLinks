using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using SmartLinks.Management.Api.Authentication;
using SmartLinks.Management.Api.Endpoints;
using SmartLinks.Management.Api.ExceptionHandling;
using SmartLinks.Management.Api.OpenApi;
using SmartLinks.Management.Application.SmartLinks.Create;
using SmartLinks.Management.Application.SmartLinks.Publication;
using SmartLinks.Management.Application.SmartLinks.Queries;
using SmartLinks.Management.Application.SmartLinks.Update;
using SmartLinks.Management.Infrastructure.DependencyInjection;
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
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlDocumentationFile =  $"{typeof(Program).Assembly.GetName().Name}.xml";
    var xmlDocumentationPath = Path.Combine(AppContext.BaseDirectory, xmlDocumentationFile);
    options.IncludeXmlComments(xmlDocumentationPath);

    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SmartLinks Management API",
        Version = "v1"
    });

    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = "X-Api-Key",
        Description = "API-ключ для изменяющих операций Management API"
    });

    options.OperationFilter<ApiKeySecurityOperationFilter>();
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(
        "/swagger/v1/swagger.json",
        "SmartLinks Management API v1");
    options.RoutePrefix = "swagger";
});
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

/// <summary>
/// Открывает точку входа Management API для интеграционных тестов
/// </summary>
public partial class Program
{
}