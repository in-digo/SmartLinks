namespace SmartLinks.Redirect.Infrastructure.Synchronization;

/// <summary>
/// Содержит настройки фоновой синхронизации конфигураций
/// </summary>
public sealed class ConfigurationSynchronizationOptions
{
    /// <summary>
    /// Возвращает интервал между запросами change feed
    /// </summary>
    public TimeSpan PollingInterval { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Возвращает максимальное количество изменений за один запрос
    /// </summary>
    public int ChangeBatchSize { get; init; } = 100;

    /// <summary>
    /// Возвращает начальную верхнюю границу задержки повторной попытки
    /// </summary>
    public TimeSpan InitialRetryDelay { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Возвращает максимальную задержку повторной попытки
    /// </summary>
    public TimeSpan MaximumRetryDelay { get; init; } = TimeSpan.FromSeconds(30);
}