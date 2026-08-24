using SmartLinks.Contracts.Configurations;
using SmartLinks.Management.Application.Abstractions;

namespace SmartLinks.Management.UnitTests.TestDoubles;

internal sealed class InMemoryConfigurationChangeLog : IConfigurationChangeLog
{
    private readonly List<ConfigurationChange> _changes = [];
    private readonly IReadOnlyList<ConfigurationChange> _readOnlyChanges;
    private long _currentRevision;

    /// <summary>
    /// Инициализирует тестовый журнал изменений
    /// </summary>
    internal InMemoryConfigurationChangeLog()
    {
        _readOnlyChanges = _changes.AsReadOnly();
    }

    public IReadOnlyList<ConfigurationChange> Changes => _readOnlyChanges;

    public int AppendCallCount { get; private set; }

    public CancellationToken LastAppendCancellationToken { get; private set; }

    /// <summary>
    /// Добавляет snapshot со следующей глобальной ревизией
    /// </summary>
    public Task<ConfigurationChange> AppendAsync(SmartLinkConfigurationSnapshot configuration, CancellationToken cancellationToken)
    {
        AppendCallCount++;
        LastAppendCancellationToken = cancellationToken;
        cancellationToken.ThrowIfCancellationRequested();

        var change = new ConfigurationChange(++_currentRevision, configuration);
        _changes.Add(change);

        return Task.FromResult(change);
    }
}