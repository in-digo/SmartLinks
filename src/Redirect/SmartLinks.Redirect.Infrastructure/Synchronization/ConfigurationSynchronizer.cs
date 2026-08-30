using SmartLinks.Redirect.Application.Configurations;
using SmartLinks.Redirect.Infrastructure.Management;

namespace SmartLinks.Redirect.Infrastructure.Synchronization;

/// <summary>
/// Синхронизирует локальный snapshot Redirect с Management API
/// </summary>
public sealed class ConfigurationSynchronizer
{
    private readonly IManagementConfigurationClient _managementConfigurationClient;
    private readonly IConfigurationSnapshotProvider _snapshotProvider;
    private readonly IConfigurationSnapshotUpdater _snapshotUpdater;

    /// <summary>
    /// Инициализирует synchronizer клиентом Management API и хранилищем snapshot
    /// </summary>
    public ConfigurationSynchronizer(
        IManagementConfigurationClient managementConfigurationClient,
        IConfigurationSnapshotProvider snapshotProvider,
        IConfigurationSnapshotUpdater snapshotUpdater)
    {
        ArgumentNullException.ThrowIfNull(managementConfigurationClient);
        ArgumentNullException.ThrowIfNull(snapshotProvider);
        ArgumentNullException.ThrowIfNull(snapshotUpdater);

        _managementConfigurationClient = managementConfigurationClient;
        _snapshotProvider = snapshotProvider;
        _snapshotUpdater = snapshotUpdater;
    }

    /// <summary>
    /// Загружает и заменяет полный snapshot конфигураций
    /// </summary>
    public async Task LoadSnapshotAsync(CancellationToken cancellationToken)
    {
        var snapshot = await _managementConfigurationClient.GetSnapshotAsync(cancellationToken);

        _snapshotUpdater.ReplaceSnapshot(snapshot);
    }

    /// <summary>
    /// Получает и применяет изменения после текущей локальной ревизии
    /// </summary>
    public async Task SynchronizeChangesAsync(int limit, CancellationToken cancellationToken)
    {
        if (limit <= 0)
            throw new ArgumentOutOfRangeException(nameof(limit), limit, "Лимит должен быть положительным");

        var changes = await _managementConfigurationClient.GetChangesAsync(
            _snapshotProvider.Revision,
            limit,
            cancellationToken);

        _snapshotUpdater.ApplyChanges(changes);
    }
}