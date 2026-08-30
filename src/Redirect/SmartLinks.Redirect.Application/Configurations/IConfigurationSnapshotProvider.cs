using System.Diagnostics.CodeAnalysis;
using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.Redirect.Application.Configurations;

/// <summary>
/// Предоставляет доступ к текущему snapshot конфигураций Redirect
/// </summary>
public interface IConfigurationSnapshotProvider
{
    /// <summary>
    /// Возвращает текущую глобальную ревизию
    /// </summary>
    long Revision { get; }

    /// <summary>
    /// Пытается получить скомпилированную конфигурацию по slug
    /// </summary>
    bool TryGetBySlug(string slug, [NotNullWhen(true)] out SmartLinkConfiguration? configuration);
}