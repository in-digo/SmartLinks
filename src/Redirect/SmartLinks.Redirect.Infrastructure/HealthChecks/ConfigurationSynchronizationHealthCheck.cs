using Microsoft.Extensions.Diagnostics.HealthChecks;
using SmartLinks.Redirect.Infrastructure.Synchronization;

namespace SmartLinks.Redirect.Infrastructure.HealthChecks;

/// <summary>
/// Проверяет готовность конфигураций Redirect
/// </summary>
public sealed class ConfigurationSynchronizationHealthCheck : IHealthCheck
{
    private readonly ConfigurationSynchronizationState _synchronizationState;

    /// <summary>
    /// Инициализирует проверку состояния синхронизации
    /// </summary>
    public ConfigurationSynchronizationHealthCheck(ConfigurationSynchronizationState synchronizationState)
    {
        ArgumentNullException.ThrowIfNull(synchronizationState);

        _synchronizationState = synchronizationState;
    }

    /// <summary>
    /// Проверяет завершение первоначальной синхронизации
    /// </summary>
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = _synchronizationState.IsReady
            ? HealthCheckResult.Healthy("Первоначальная синхронизация конфигураций завершена")
            : HealthCheckResult.Unhealthy("Первоначальная синхронизация конфигураций не завершена");

        return Task.FromResult(result);
    }
}