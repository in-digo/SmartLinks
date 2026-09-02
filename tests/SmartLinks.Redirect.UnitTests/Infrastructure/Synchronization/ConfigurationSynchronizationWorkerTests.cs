using System.Diagnostics.CodeAnalysis;
using SmartLinks.Contracts.Configurations;
using SmartLinks.Redirect.Application.Configurations;
using SmartLinks.Redirect.Infrastructure.Management;
using SmartLinks.Redirect.Infrastructure.Synchronization;
using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.Redirect.UnitTests.Infrastructure.Synchronization;

public sealed class ConfigurationSynchronizationWorkerTests
{
    /// <summary>
    /// Проверяет загрузку полного snapshot сразу после запуска worker
    /// </summary>
    [Fact]
    public async Task StartAsyncLoadsFullSnapshot()
    {
        var expectedSnapshot = new PublishedSmartLinksSnapshot(7, []);
        var client = new StubManagementConfigurationClient(expectedSnapshot);
        var snapshotProvider = new StubConfigurationSnapshotProvider();
        var snapshotUpdater = new RecordingConfigurationSnapshotUpdater();
        var synchronizer = new ConfigurationSynchronizer(
            client,
            snapshotProvider,
            snapshotUpdater);
        using var worker = new ConfigurationSynchronizationWorker(synchronizer);

        await worker.StartAsync(CancellationToken.None);

        Assert.Equal(1, client.SnapshotRequestCount);
        Assert.Same(expectedSnapshot, snapshotUpdater.ReplacedSnapshot);
    }

    /// <summary>
    /// Проверяет ошибку создания worker без synchronizer
    /// </summary>
    [Fact]
    public void ConstructorWithNullSynchronizerThrowsArgumentNullException()
    {
        ConfigurationSynchronizer synchronizer = null!;

        var exception = Assert.Throws<ArgumentNullException>(
            () => new ConfigurationSynchronizationWorker(synchronizer));

        Assert.Equal("synchronizer", exception.ParamName);
    }

    private sealed class StubManagementConfigurationClient : IManagementConfigurationClient
    {
        private readonly PublishedSmartLinksSnapshot _snapshot;

        /// <summary>
        /// Инициализирует клиент возвращаемым snapshot
        /// </summary>
        public StubManagementConfigurationClient(PublishedSmartLinksSnapshot snapshot)
        {
            _snapshot = snapshot;
        }

        public int SnapshotRequestCount { get; private set; }

        /// <summary>
        /// Возвращает подготовленный полный snapshot
        /// </summary>
        public Task<PublishedSmartLinksSnapshot> GetSnapshotAsync(
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            SnapshotRequestCount++;

            return Task.FromResult(_snapshot);
        }

        /// <summary>
        /// Отклоняет неиспользуемый запрос изменений
        /// </summary>
        public Task<IReadOnlyList<ConfigurationChange>> GetChangesAsync(
            long afterRevision,
            int limit,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException("Получение изменений не используется в этом тесте");
        }
    }

    private sealed class StubConfigurationSnapshotProvider : IConfigurationSnapshotProvider
    {
        public long Revision => 0;

        /// <summary>
        /// Возвращает отсутствие конфигурации для неиспользуемого поиска
        /// </summary>
        public bool TryGetBySlug(
            string slug,
            [NotNullWhen(true)] out SmartLinkConfiguration? configuration)
        {
            configuration = null;
            return false;
        }
    }

    private sealed class RecordingConfigurationSnapshotUpdater : IConfigurationSnapshotUpdater
    {
        public PublishedSmartLinksSnapshot? ReplacedSnapshot { get; private set; }

        /// <summary>
        /// Запоминает переданный полный snapshot
        /// </summary>
        public void ReplaceSnapshot(PublishedSmartLinksSnapshot snapshot)
        {
            ReplacedSnapshot = snapshot;
        }

        /// <summary>
        /// Отклоняет неиспользуемое применение изменений
        /// </summary>
        public void ApplyChanges(IReadOnlyList<ConfigurationChange> changes)
        {
            throw new NotSupportedException("Применение изменений не используется в этом тесте");
        }
    }
}