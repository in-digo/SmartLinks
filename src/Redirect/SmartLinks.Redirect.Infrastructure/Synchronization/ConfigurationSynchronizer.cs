using SmartLinks.Redirect.Application.Configurations;
using SmartLinks.Redirect.Infrastructure.Management;

namespace SmartLinks.Redirect.Infrastructure.Synchronization;

/// <summary>
/// Синхронизирует локальный snapshot Redirect с Management API
/// </summary>
public sealed class ConfigurationSynchronizer
{
    private readonly IManagementConfigurationClient _managementConfigurationClient;
    private readonly IConfigurationSnapshotUpdater _snapshotUpdater;

    /// <summary>
    /// Инициализирует synchronizer клиентом Management API и хранилищем snapshot
    /// </summary>
    public ConfigurationSynchronizer(
        IManagementConfigurationClient managementConfigurationClient,
        IConfigurationSnapshotUpdater snapshotUpdater)
    {
        ArgumentNullException.ThrowIfNull(managementConfigurationClient);
        ArgumentNullException.ThrowIfNull(snapshotUpdater);

        _managementConfigurationClient = managementConfigurationClient;
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
}