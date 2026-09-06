using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartLinks.Management.Application.Abstractions;
using SmartLinks.Management.Infrastructure.DependencyInjection;
using SmartLinks.Management.Infrastructure.Persistence;
using SmartLinks.Management.Infrastructure.Persistence.Publication;
using SmartLinks.Management.Infrastructure.Persistence.Repositories;

namespace SmartLinks.Management.IntegrationTests.DependencyInjection;

public sealed class ManagementInfrastructureServiceCollectionExtensionsTests
{
    private const string _connectionString = "Host=localhost;Database=smartlinks";

    /// <summary>
    /// Проверяет регистрацию PostgreSQL-репозитория умных ссылок
    /// </summary>
    [Fact]
    public void AddManagementInfrastructureRegistersSmartLinkRepository()
    {
        var services = new ServiceCollection();

        services.AddManagementInfrastructure(_connectionString);

        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        var repository = scope.ServiceProvider.GetRequiredService<ISmartLinkRepository>();

        Assert.IsType<SmartLinkRepository>(repository);
    }

    /// <summary>
    /// Проверяет регистрацию журнала и reader опубликованных конфигураций
    /// </summary>
    [Fact]
    public void AddManagementInfrastructureRegistersPublicationServices()
    {
        var services = new ServiceCollection();

        services.AddManagementInfrastructure(_connectionString);

        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        var changeLog = scope.ServiceProvider.GetRequiredService<IConfigurationChangeLog>();
        var reader = scope.ServiceProvider.GetRequiredService<IPublishedConfigurationReader>();

        Assert.IsType<ConfigurationChangeLog>(changeLog);
        Assert.IsType<PublishedConfigurationReader>(reader);
    }

    /// <summary>
    /// Проверяет настройку контекста для PostgreSQL
    /// </summary>
    [Fact]
    public void AddManagementInfrastructureConfiguresPostgreSqlContext()
    {
        var services = new ServiceCollection();

        services.AddManagementInfrastructure(_connectionString);

        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ManagementDbContext>();

        Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", dbContext.Database.ProviderName);
        Assert.Equal(_connectionString, dbContext.Database.GetConnectionString());
    }

    /// <summary>
    /// Проверяет scoped lifetime компонентов хранения
    /// </summary>
    [Fact]
    public void AddManagementInfrastructureRegistersPersistenceServicesAsScoped()
    {
        var services = new ServiceCollection();

        services.AddManagementInfrastructure(_connectionString);

        AssertScoped<ManagementDbContext>(services);
        AssertScoped<ISmartLinkRepository>(services);
        AssertScoped<IConfigurationChangeLog>(services);
        AssertScoped<IPublishedConfigurationReader>(services);
    }

    /// <summary>
    /// Проверяет отклонение пустой строки подключения
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void AddManagementInfrastructureWithInvalidConnectionStringThrowsArgumentException(string? connectionString)
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<ArgumentException>(
            () => services.AddManagementInfrastructure(connectionString!));

        Assert.Equal("connectionString", exception.ParamName);
    }

    /// <summary>
    /// Проверяет scoped lifetime указанного сервиса
    /// </summary>
    private static void AssertScoped<TService>(IServiceCollection services)
    {
        var descriptor = services.Single(serviceDescriptor => serviceDescriptor.ServiceType == typeof(TService));

        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }
}