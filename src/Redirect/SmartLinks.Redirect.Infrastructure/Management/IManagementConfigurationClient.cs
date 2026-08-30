using SmartLinks.Contracts.Configurations;

namespace SmartLinks.Redirect.Infrastructure.Management;

/// <summary>
/// Предоставляет опубликованные конфигурации Management API
/// </summary>
public interface IManagementConfigurationClient
{
    /// <summary>
    /// Получает полный snapshot опубликованных конфигураций
    /// </summary>
    Task<PublishedSmartLinksSnapshot> GetSnapshotAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Получает изменения опубликованных конфигураций после указанной ревизии
    /// </summary>
    Task<IReadOnlyList<ConfigurationChange>> GetChangesAsync(
        long afterRevision,
        int limit,
        CancellationToken cancellationToken);
}