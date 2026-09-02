using Microsoft.Extensions.Hosting;

namespace SmartLinks.Redirect.Infrastructure.Synchronization;

/// <summary>
/// Выполняет фоновую синхронизацию конфигураций Redirect
/// </summary>
public sealed class ConfigurationSynchronizationWorker : BackgroundService
{
    private readonly ConfigurationSynchronizer _synchronizer;

    /// <summary>
    /// Инициализирует фоновую синхронизацию
    /// </summary>
    public ConfigurationSynchronizationWorker(ConfigurationSynchronizer synchronizer)
    {
        ArgumentNullException.ThrowIfNull(synchronizer);

        _synchronizer = synchronizer;
    }

    /// <summary>
    /// Запускает фоновую синхронизацию конфигураций
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _synchronizer.LoadSnapshotAsync(stoppingToken);
    }
}