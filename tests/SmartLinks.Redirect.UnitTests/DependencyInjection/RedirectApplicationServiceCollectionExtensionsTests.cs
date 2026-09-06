using Microsoft.Extensions.DependencyInjection;
using SmartLinks.Redirect.Application.Configurations;
using SmartLinks.Redirect.Application.DependencyInjection;
using SmartLinks.RuleEngine.Conditions;

namespace SmartLinks.Redirect.UnitTests.DependencyInjection;

public sealed class RedirectApplicationServiceCollectionExtensionsTests
{
    /// <summary>
    /// Проверяет регистрацию единственного хранилища для интерфейсов чтения и обновления
    /// </summary>
    [Fact]
    public void AddRedirectApplicationRegistersSharedConfigurationSnapshotStore()
    {
        var services = new ServiceCollection();

        services.AddRedirectApplication();

        using var serviceProvider = services.BuildServiceProvider();
        var snapshotProvider = serviceProvider.GetRequiredService<IConfigurationSnapshotProvider>();
        var snapshotUpdater = serviceProvider.GetRequiredService<IConfigurationSnapshotUpdater>();

        Assert.IsType<ConfigurationSnapshotStore>(snapshotProvider);
        Assert.Same(snapshotProvider, snapshotUpdater);
    }

    /// <summary>
    /// Проверяет регистрацию обязательных зависимостей Rule Engine
    /// </summary>
    [Fact]
    public void AddRedirectApplicationRegistersRequiredRuleEngineServices()
    {
        var services = new ServiceCollection();

        services.AddRedirectApplication();

        using var serviceProvider = services.BuildServiceProvider();

        Assert.IsType<ConditionDslCompiler>(serviceProvider.GetRequiredService<ConditionDslCompiler>());
    }

    /// <summary>
    /// Проверяет единый экземпляр хранилища во всех DI scopes
    /// </summary>
    [Fact]
    public void AddRedirectApplicationRegistersConfigurationSnapshotStoreAsSingleton()
    {
        var services = new ServiceCollection();
        services.AddRedirectApplication();

        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        var rootProvider = serviceProvider.GetRequiredService<IConfigurationSnapshotProvider>();
        var scopedProvider = scope.ServiceProvider.GetRequiredService<IConfigurationSnapshotProvider>();

        Assert.Same(rootProvider, scopedProvider);
    }

    /// <summary>
    /// Проверяет ошибку регистрации для отсутствующей коллекции сервисов
    /// </summary>
    [Fact]
    public void AddRedirectApplicationWithNullServicesThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        Assert.Throws<ArgumentNullException>(() => services.AddRedirectApplication());
    }
}