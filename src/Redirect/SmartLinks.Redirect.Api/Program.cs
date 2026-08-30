using SmartLinks.Redirect.Application.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRedirectApplication();
var app = builder.Build();

app.Run();

// Открывает точку входа Redirect API для интеграционных тестов
public partial class Program
{
}