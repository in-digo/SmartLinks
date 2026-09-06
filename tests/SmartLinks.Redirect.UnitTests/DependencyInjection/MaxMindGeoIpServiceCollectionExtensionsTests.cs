using Microsoft.Extensions.DependencyInjection;
using SmartLinks.Redirect.Application.DependencyInjection;
using SmartLinks.Redirect.Infrastructure.DependencyInjection;
using SmartLinks.Redirect.Infrastructure.GeoIp;
using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.Redirect.UnitTests.DependencyInjection;

public sealed class MaxMindGeoIpServiceCollectionExtensionsTests
{
    /// <summary>
    /// Проверяет замену стандартного определителя страны реализацией MaxMind
    /// </summary>
    [Fact]
    public void AddMaxMindGeoIpReplacesDefaultClientLocationResolver()
    {
        var services = new ServiceCollection();

        services.AddRedirectApplication();
        services.AddMaxMindGeoIp(GetDatabasePath());

        using var serviceProvider = services.BuildServiceProvider();
        var resolvers = serviceProvider.GetServices<IClientLocationResolver>();
        var resolver = Assert.Single(resolvers);

        Assert.IsType<MaxMindClientLocationResolver>(resolver);
    }

    /// <summary>
    /// Проверяет singleton-регистрацию определителя страны MaxMind
    /// </summary>
    [Fact]
    public void AddMaxMindGeoIpRegistersSingletonClientLocationResolver()
    {
        var services = new ServiceCollection();

        services.AddRedirectApplication();
        services.AddMaxMindGeoIp(GetDatabasePath());

        using var serviceProvider = services.BuildServiceProvider();
        var firstResolver = serviceProvider.GetRequiredService<IClientLocationResolver>();
        var secondResolver = serviceProvider.GetRequiredService<IClientLocationResolver>();

        Assert.Same(firstResolver, secondResolver);
    }

    /// <summary>
    /// Проверяет ошибку регистрации GeoIP без коллекции сервисов
    /// </summary>
    [Fact]
    public void AddMaxMindGeoIpWithNullServicesThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var exception = Assert.Throws<ArgumentNullException>(
            () => services.AddMaxMindGeoIp(GetDatabasePath()));

        Assert.Equal("services", exception.ParamName);
    }

    /// <summary>
    /// Проверяет ошибку регистрации GeoIP без пути к базе MaxMind
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void AddMaxMindGeoIpWithMissingDatabasePathThrowsArgumentException(string? databasePath)
    {
        var services = new ServiceCollection();

        var exception = Assert.ThrowsAny<ArgumentException>(
            () => services.AddMaxMindGeoIp(databasePath!));

        Assert.Equal("databasePath", exception.ParamName);
    }

    /// <summary>
    /// Возвращает путь к тестовой базе MaxMind
    /// </summary>
    private static string GetDatabasePath()
    {
        return Path.Combine(AppContext.BaseDirectory, "TestData", "GeoIP2-Country-Test.mmdb");
    }
}