using Microsoft.Extensions.Options;

namespace SmartLinks.Redirect.Infrastructure.Synchronization;

/// <summary>
/// Рассчитывает задержку повторной синхронизации с exponential backoff и jitter
/// </summary>
public sealed class ConfigurationSynchronizationRetryDelayProvider
{
    private readonly ConfigurationSynchronizationOptions _options;
    private readonly Random _random;

    /// <summary>
    /// Инициализирует расчёт задержки настройками синхронизации и генератором jitter
    /// </summary>
    public ConfigurationSynchronizationRetryDelayProvider(
        IOptions<ConfigurationSynchronizationOptions> options,
        Random random)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(random);

        _options = options.Value;
        _random = random;
    }

    /// <summary>
    /// Рассчитывает задержку для указанной повторной попытки
    /// </summary>
    public TimeSpan GetDelay(int retryAttempt)
    {
        if (retryAttempt <= 0)
            throw new ArgumentOutOfRangeException(nameof(retryAttempt), retryAttempt, "Номер повторной попытки должен быть положительным");

        var exponentialDelayMilliseconds = _options.InitialRetryDelay.TotalMilliseconds * Math.Pow(2, retryAttempt - 1);
        var retryWindowMilliseconds = Math.Min(exponentialDelayMilliseconds, _options.MaximumRetryDelay.TotalMilliseconds);
        var delayMilliseconds = retryWindowMilliseconds * _random.NextDouble();

        return TimeSpan.FromMilliseconds(delayMilliseconds);
    }
}