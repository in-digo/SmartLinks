using Testcontainers.PostgreSql;

namespace SmartLinks.Management.IntegrationTests.Infrastructure;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("smartlinks_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();
        
    public string ConnectionString => _container.GetConnectionString();

    /// <summary>
    /// Запускает контейнер PostgreSQL перед интеграционными тестами
    /// </summary>
    public Task InitializeAsync() => _container.StartAsync();

    /// <summary>
    /// Останавливает и удаляет контейнер PostgreSQL после интеграционных тестов
    /// </summary>
    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}