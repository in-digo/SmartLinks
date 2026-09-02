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
    private readonly ConfigurationSynchronizationRetryDelayProvider _retryDelayProvider;
    private readonly ConfigurationSynchronizationState _synchronizationState;

    /// <summary>
    /// Инициализирует фоновую синхронизацию
    /// </summary>
    public ConfigurationSynchronizationWorker(
        ConfigurationSynchronizer synchronizer,
        IOptions<ConfigurationSynchronizationOptions> options,
        TimeProvider timeProvider,
        ConfigurationSynchronizationRetryDelayProvider retryDelayProvider,
        ConfigurationSynchronizationState synchronizationState)
    {
        ArgumentNullException.ThrowIfNull(synchronizer);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(retryDelayProvider);
        ArgumentNullException.ThrowIfNull(synchronizationState);

        _synchronizer = synchronizer;
        _options = options.Value;
        _timeProvider = timeProvider;
        _retryDelayProvider = retryDelayProvider;
        _synchronizationState = synchronizationState;
    }

    /// <summary>
    /// Запускает фоновую синхронизацию конфигураций
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ExecuteWithRetryAsync(_synchronizer.LoadSnapshotAsync, stoppingToken);

        _synchronizationState.MarkReady();
        
        using var timer = new PeriodicTimer(_options.PollingInterval, _timeProvider);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ExecuteWithRetryAsync(
                cancellationToken => _synchronizer.SynchronizeChangesAsync(_options.ChangeBatchSize, cancellationToken),
                stoppingToken);
        }
    }

    /// <summary>
    /// Повторяет операцию после временных ошибок Management API
    /// </summary>
    private async Task ExecuteWithRetryAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken)
    {
        var retryAttempt = 1;

        while (true)
        {
            try
            {
                await operation(cancellationToken);
                return;
            }
            catch (HttpRequestException) when (!cancellationToken.IsCancellationRequested)
            {
                var retryDelay = _retryDelayProvider.GetDelay(retryAttempt);
                retryAttempt++;

                await Task.Delay(retryDelay, _timeProvider, cancellationToken);
            }
        }
    }
}