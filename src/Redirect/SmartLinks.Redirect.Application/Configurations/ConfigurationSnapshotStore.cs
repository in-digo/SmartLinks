using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using SmartLinks.Contracts.Configurations;
using SmartLinks.RuleEngine.Conditions;
using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.Redirect.Application.Configurations;

/// <summary>
/// Хранит текущий неизменяемый snapshot конфигураций Redirect
/// </summary>
public sealed class ConfigurationSnapshotStore :
    IConfigurationSnapshotProvider,
    IConfigurationSnapshotUpdater
{
    private readonly ConditionDslCompiler _conditionDslCompiler;
    private SnapshotState _currentSnapshot = new(
        0,
        ImmutableDictionary.Create<string, CompiledSmartLinkConfiguration>(StringComparer.OrdinalIgnoreCase),
        ImmutableDictionary<Guid, string>.Empty);

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

        var compiledConfigurations = snapshot.Configurations
            .Select(CompileConfiguration)
            .ToImmutableArray();

        var configurations = compiledConfigurations.ToImmutableDictionary(
            configuration => configuration.Slug,
            configuration => configuration,
            StringComparer.OrdinalIgnoreCase);

        var slugsById = compiledConfigurations.ToImmutableDictionary(
            configuration => configuration.Id,
            configuration => configuration.Slug);

        var nextSnapshot = new SnapshotState(
            snapshot.Revision,
            configurations,
            slugsById);

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
    /// Компилирует и применяет последовательные изменения конфигураций
    /// </summary>
    public void ApplyChanges(IReadOnlyList<ConfigurationChange> changes)
    {
        ArgumentNullException.ThrowIfNull(changes);

        if (changes.Count == 0)
            return;

        while (true)
        {
            var currentSnapshot = Volatile.Read(ref _currentSnapshot);
            var nextSnapshot = CreateChangedSnapshot(
                currentSnapshot,
                changes);

            if (ReferenceEquals(nextSnapshot, currentSnapshot))
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

        if (snapshot.Configurations.TryGetValue(
            slug,
            out var compiledConfiguration))
        {
            configuration = compiledConfiguration.Configuration;
            return true;
        }

        configuration = null;
        return false;
    }

    /// <summary>
    /// Формирует новый snapshot из непрерывной последовательности изменений
    /// </summary>
    private SnapshotState CreateChangedSnapshot(SnapshotState currentSnapshot, IReadOnlyList<ConfigurationChange> changes)
    {
        var revision = currentSnapshot.Revision;
        var configurations = currentSnapshot.Configurations;
        var slugsById = currentSnapshot.SlugsById;
        var hasApplicableChanges = false;

        foreach (var change in changes)
        {
            if (change.Revision <= revision)
                continue;

            var expectedRevision = revision + 1;
            if (change.Revision != expectedRevision)
            {
                throw new InvalidOperationException(
                    FormattableString.Invariant(
                        $"Ожидалась ревизия '{expectedRevision}', но получена '{change.Revision}'"));
            }

            var compiledConfiguration = CompileConfiguration(
                change.Configuration);

            if (slugsById.TryGetValue(
                compiledConfiguration.Id,
                out var previousSlug))
            {
                configurations = configurations.Remove(previousSlug);
            }

            if (configurations.TryGetValue(
                    compiledConfiguration.Slug,
                    out var existingConfiguration) &&
                existingConfiguration.Id != compiledConfiguration.Id)
            {
                throw new InvalidOperationException(
                    $"Короткий адрес '{compiledConfiguration.Slug}' уже принадлежит другой умной ссылке");
            }

            configurations = configurations.SetItem(
                compiledConfiguration.Slug,
                compiledConfiguration);

            slugsById = slugsById.SetItem(
                compiledConfiguration.Id,
                compiledConfiguration.Slug);

            revision = change.Revision;
            hasApplicableChanges = true;
        }

        if (!hasApplicableChanges)
            return currentSnapshot;

        return new SnapshotState(
            revision,
            configurations,
            slugsById);
    }

    /// <summary>
    /// Компилирует опубликованную конфигурацию в готовую read-модель
    /// </summary>
    private CompiledSmartLinkConfiguration CompileConfiguration(SmartLinkConfigurationSnapshot configuration)
    {
        var rules = configuration.Rules
            .Select(rule => new SmartLinkRule(
                rule.Priority,
                rule.IsEnabled,
                rule.TargetUrl,
                _conditionDslCompiler.Compile(rule.ConditionDsl)))
            .ToImmutableArray();

        var smartLinkConfiguration = new SmartLinkConfiguration(
            configuration.IsActive,
            configuration.DefaultUrl,
            rules);

        return new CompiledSmartLinkConfiguration(
            configuration.Id,
            configuration.Slug,
            smartLinkConfiguration);
    }

    private sealed record CompiledSmartLinkConfiguration(
        Guid Id,
        string Slug,
        SmartLinkConfiguration Configuration);

    private sealed record SnapshotState(
        long Revision,
        ImmutableDictionary<string, CompiledSmartLinkConfiguration> Configurations,
        ImmutableDictionary<Guid, string> SlugsById);
}