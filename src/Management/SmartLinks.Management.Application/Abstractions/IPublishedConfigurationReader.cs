using SmartLinks.Contracts.Configurations;

namespace SmartLinks.Management.Application.Abstractions;

public interface IPublishedConfigurationReader
{
    /// <summary>
    /// Возвращает полный snapshot опубликованных конфигураций
    /// </summary>
    Task<PublishedSmartLinksSnapshot> GetSnapshotAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Возвращает изменения после указанной глобальной ревизии
    /// </summary>
    Task<IReadOnlyList<ConfigurationChange>> GetChangesAsync(
        long afterRevision,
        int limit,
        CancellationToken cancellationToken);
}