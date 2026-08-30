using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using SmartLinks.Contracts.Configurations;
using SmartLinks.RuleEngine.Conditions;
using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.Redirect.Application.Configurations;

/// <summary>
/// Хранит текущий неизменяемый snapshot конфигураций Redirect
/// </summary>
public sealed class ConfigurationSnapshotStore : IConfigurationSnapshotProvider
{
    private readonly ConditionDslCompiler _conditionDslCompiler;
    private SnapshotState _currentSnapshot = new(0, ImmutableDictionary.Create<string, SmartLinkConfiguration>(StringComparer.OrdinalIgnoreCase));

    /// <summary>
    /// Инициализирует хранилище компилятором DSL
    /// </summary>
    public ConfigurationSnapshotStore(ConditionDslCompiler conditionDslCompiler)
    {
        ArgumentNullException.ThrowIfNull(conditionDslCompiler);

        _conditionDslCompiler = conditionDslCompiler;
    }

    /// <summary>
    /// Возвращает текущую глобальную ревизию
    /// </summary>
    public long Revision => Volatile.Read(ref _currentSnapshot).Revision;

    /// <summary>
    /// Компилирует и атомарно заменяет полный snapshot конфигураций
    /// </summary>
    public void ReplaceSnapshot(PublishedSmartLinksSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var currentSnapshot = Volatile.Read(ref _currentSnapshot);
        if (snapshot.Revision <= currentSnapshot.Revision)
            return;

        var configurations = snapshot.Configurations.ToImmutableDictionary(
            configuration => configuration.Slug,
            CompileConfiguration,
            StringComparer.OrdinalIgnoreCase);

        var nextSnapshot = new SnapshotState(snapshot.Revision, configurations);

        while (true)
        {
            currentSnapshot = Volatile.Read(ref _currentSnapshot);
            if (snapshot.Revision <= currentSnapshot.Revision)
                return;

            var replacedSnapshot = Interlocked.CompareExchange(
                ref _currentSnapshot,
                nextSnapshot,
                currentSnapshot);

            if (ReferenceEquals(replacedSnapshot, currentSnapshot))
                return;
        }
    }

    /// <summary>
    /// Пытается получить скомпилированную конфигурацию по slug
    /// </summary>
    public bool TryGetBySlug(string slug, [NotNullWhen(true)] out SmartLinkConfiguration? configuration)
    {
        var snapshot = Volatile.Read(ref _currentSnapshot);

        return snapshot.Configurations.TryGetValue(slug, out configuration);
    }

    /// <summary>
    /// Преобразует опубликованную конфигурацию в готовую read-модель
    /// </summary>
    private SmartLinkConfiguration CompileConfiguration(
        SmartLinkConfigurationSnapshot configuration)
    {
        var rules = configuration.Rules
            .Select(rule => new SmartLinkRule(
                rule.Priority,
                rule.IsEnabled,
                rule.TargetUrl,
                _conditionDslCompiler.Compile(rule.ConditionDsl)))
            .ToImmutableArray();

        return new SmartLinkConfiguration(
            configuration.IsActive,
            configuration.DefaultUrl,
            rules);
    }

    private sealed record SnapshotState(
        long Revision,
        ImmutableDictionary<string, SmartLinkConfiguration> Configurations);
}