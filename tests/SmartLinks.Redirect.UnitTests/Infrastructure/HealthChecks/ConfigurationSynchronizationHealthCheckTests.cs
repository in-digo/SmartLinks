using Microsoft.Extensions.Diagnostics.HealthChecks;
using SmartLinks.Redirect.Infrastructure.HealthChecks;
using SmartLinks.Redirect.Infrastructure.Synchronization;

namespace SmartLinks.Redirect.UnitTests.Infrastructure.HealthChecks;

public sealed class ConfigurationSynchronizationHealthCheckTests
{
    /// <summary>
    /// Проверяет неготовность до завершения первоначальной синхронизации
    /// </summary>
    [Fact]
    public async Task CheckHealthAsyncReturnsUnhealthyBeforeInitialSynchronization()
    {
        var synchronizationState = new ConfigurationSynchronizationState();
        var healthCheck = new ConfigurationSynchronizationHealthCheck(synchronizationState);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("Первоначальная синхронизация конфигураций не завершена", result.Description);
    }

    /// <summary>
    /// Проверяет готовность после завершения первоначальной синхронизации
    /// </summary>
    [Fact]
    public async Task CheckHealthAsyncReturnsHealthyAfterInitialSynchronization()
    {
        var synchronizationState = new ConfigurationSynchronizationState();
        synchronizationState.MarkReady();
        var healthCheck = new ConfigurationSynchronizationHealthCheck(synchronizationState);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("Первоначальная синхронизация конфигураций завершена", result.Description);
    }

    /// <summary>
    /// Проверяет ошибку создания health check без состояния синхронизации
    /// </summary>
    [Fact]
    public void ConstructorWithNullSynchronizationStateThrowsArgumentNullException()
    {
        ConfigurationSynchronizationState synchronizationState = null!;

        var exception = Assert.Throws<ArgumentNullException>(() => new ConfigurationSynchronizationHealthCheck(synchronizationState));

        Assert.Equal("synchronizationState", exception.ParamName);
    }

    /// <summary>
    /// Проверяет отмену проверки готовности
    /// </summary>
    [Fact]
    public async Task CheckHealthAsyncWithCancelledTokenThrowsOperationCanceledException()
    {
        var synchronizationState = new ConfigurationSynchronizationState();
        var healthCheck = new ConfigurationSynchronizationHealthCheck(synchronizationState);
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => healthCheck.CheckHealthAsync(new HealthCheckContext(), cancellationTokenSource.Token));
    }
}