using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace SmartLinks.Redirect.Infrastructure.Synchronization;

/// <summary>
/// Выполняет фоновую синхронизацию конфигураций Redirect
/// </summary>
public sealed class ConfigurationSynchronizationWorker : BackgroundService
{
    private readonly ConfigurationSynchronizer _synchronizer;
    private readonly ConfigurationSynchronizationOptions _options;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Инициализирует фоновую синхронизацию
    /// </summary>
    public ConfigurationSynchronizationWorker(
        ConfigurationSynchronizer synchronizer,
        IOptions<ConfigurationSynchronizationOptions> options,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(synchronizer);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _synchronizer = synchronizer;
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Запускает фоновую синхронизацию конфигураций
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _synchronizer.LoadSnapshotAsync(stoppingToken);

        using var timer = new PeriodicTimer(_options.PollingInterval, _timeProvider);

        while (await timer.WaitForNextTickAsync(stoppingToken))
            await _synchronizer.SynchronizeChangesAsync(_options.ChangeBatchSize, stoppingToken);
    }
}