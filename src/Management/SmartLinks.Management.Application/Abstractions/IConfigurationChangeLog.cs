using SmartLinks.Contracts.Configurations;

namespace SmartLinks.Management.Application.Abstractions;

public interface IConfigurationChangeLog
{
    /// <summary>
    /// Добавляет snapshot конфигурации и возвращает запись со следующей глобальной ревизией
    /// </summary>
    Task<ConfigurationChange> AppendAsync(SmartLinkConfigurationSnapshot configuration, CancellationToken cancellationToken);
}