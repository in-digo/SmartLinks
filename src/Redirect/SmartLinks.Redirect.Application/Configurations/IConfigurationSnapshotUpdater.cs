using SmartLinks.Contracts.Configurations;

namespace SmartLinks.Redirect.Application.Configurations;

/// <summary>
/// Обновляет текущий snapshot конфигураций Redirect
/// </summary>
public interface IConfigurationSnapshotUpdater
{
    /// <summary>
    /// Компилирует и атомарно заменяет полный snapshot конфигураций
    /// </summary>
    void ReplaceSnapshot(PublishedSmartLinksSnapshot snapshot);

    /// <summary>
    /// Компилирует и применяет последовательные изменения конфигураций
    /// </summary>
    void ApplyChanges(IReadOnlyList<ConfigurationChange> changes);
}