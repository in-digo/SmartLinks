using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    // Liveness проверяет только работоспособность процесса и не зависит от PostgreSQL
    Predicate = _ => false
});

app.Run();

// Открывает точку входа Management API для интеграционных тестов
public partial class Program
{
}