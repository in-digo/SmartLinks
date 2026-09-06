namespace SmartLinks.Redirect.Infrastructure.Synchronization;

/// <summary>
/// Хранит состояние первоначальной синхронизации Redirect
/// </summary>
public sealed class ConfigurationSynchronizationState
{
    private bool _isReady;

    /// <summary>
    /// Возвращает признак готовности локальных конфигураций
    /// </summary>
    public bool IsReady => Volatile.Read(ref _isReady);

    /// <summary>
    /// Отмечает завершение первоначальной синхронизации
    /// </summary>
    public void MarkReady()
    {
        Volatile.Write(ref _isReady, true);
    }
}