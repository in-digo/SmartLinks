using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
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
        var snapshotProvider = new StubConfigurationSnapshotProvider(0);
        var snapshotUpdater = new RecordingConfigurationSnapshotUpdater();
        var synchronizer = new ConfigurationSynchronizer(client, snapshotProvider, snapshotUpdater);

        using var worker = new ConfigurationSynchronizationWorker(
            synchronizer,
            Options.Create(new ConfigurationSynchronizationOptions()),
            TimeProvider.System,
            CreateRetryDelayProvider(),
            new ConfigurationSynchronizationState());

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

        var exception = Assert.Throws<ArgumentNullException>(() => new ConfigurationSynchronizationWorker(
            synchronizer,
            Options.Create(new ConfigurationSynchronizationOptions()),
            TimeProvider.System,
            CreateRetryDelayProvider(),
            new ConfigurationSynchronizationState()));

        Assert.Equal("synchronizer", exception.ParamName);
    }

    /// <summary>
    /// Проверяет получение изменений после истечения интервала опроса
    /// </summary>
    [Fact]
    public async Task WorkerSynchronizesChangesAfterPollingInterval()
    {
        var pollingInterval = TimeSpan.FromMinutes(1);
        IReadOnlyList<ConfigurationChange> expectedChanges =
        [
            new ConfigurationChange(
                8,
                new SmartLinkConfigurationSnapshot(
                    Guid.NewGuid(),
                    "summer-sale",
                    "https://example.com/default",
                    true,
                    []))
        ];
        var client = new StubManagementConfigurationClient(new PublishedSmartLinksSnapshot(7, []), expectedChanges);
        var snapshotProvider = new StubConfigurationSnapshotProvider(7);
        var snapshotUpdater = new RecordingConfigurationSnapshotUpdater();
        var synchronizer = new ConfigurationSynchronizer(client, snapshotProvider, snapshotUpdater);
        var timeProvider = new FakeTimeProvider();
        var options = Options.Create(new ConfigurationSynchronizationOptions
        {
            PollingInterval = pollingInterval,
            ChangeBatchSize = 50
        });

        using var worker = new ConfigurationSynchronizationWorker(
            synchronizer,
            options,
            timeProvider,
            CreateRetryDelayProvider(),
            new ConfigurationSynchronizationState());

        await worker.StartAsync(CancellationToken.None);

        Assert.Equal(1, client.SnapshotRequestCount);
        Assert.Equal(0, client.ChangesRequestCount);

        timeProvider.Advance(pollingInterval);
        await client.WaitForChangesRequestAsync();

        Assert.Equal(1, client.ChangesRequestCount);
        Assert.Equal(7, client.ChangesAfterRevision);
        Assert.Equal(50, client.ChangesLimit);
        Assert.Same(expectedChanges, snapshotUpdater.AppliedChanges);

        await worker.StopAsync(CancellationToken.None);
    }

    /// <summary>
    /// Проверяет ошибку создания worker без настроек синхронизации
    /// </summary>
    [Fact]
    public void ConstructorWithNullOptionsThrowsArgumentNullException()
    {
        IOptions<ConfigurationSynchronizationOptions> options = null!;

        var exception = Assert.Throws<ArgumentNullException>(() => new ConfigurationSynchronizationWorker(
            CreateSynchronizer(),
            options,
            TimeProvider.System,
            CreateRetryDelayProvider(),
            new ConfigurationSynchronizationState()));

        Assert.Equal("options", exception.ParamName);
    }

    /// <summary>
    /// Проверяет ошибку создания worker без источника времени
    /// </summary>
    [Fact]
    public void ConstructorWithNullTimeProviderThrowsArgumentNullException()
    {
        TimeProvider timeProvider = null!;

        var exception = Assert.Throws<ArgumentNullException>(() => new ConfigurationSynchronizationWorker(
            CreateSynchronizer(),
            Options.Create(new ConfigurationSynchronizationOptions()),
            timeProvider,
            CreateRetryDelayProvider(),
            new ConfigurationSynchronizationState()));

        Assert.Equal("timeProvider", exception.ParamName);
    }

    /// <summary>
    /// Проверяет синхронизацию изменений после каждого интервала опроса
    /// </summary>
    [Fact]
    public async Task WorkerSynchronizesChangesAfterEveryPollingInterval()
    {
        var pollingInterval = TimeSpan.FromMinutes(1);
        var client = new StubManagementConfigurationClient(new PublishedSmartLinksSnapshot(7, []));
        var snapshotProvider = new StubConfigurationSnapshotProvider(7);
        var snapshotUpdater = new RecordingConfigurationSnapshotUpdater();
        var synchronizer = new ConfigurationSynchronizer(client, snapshotProvider, snapshotUpdater);
        var timeProvider = new FakeTimeProvider();
        var options = Options.Create(new ConfigurationSynchronizationOptions
        {
            PollingInterval = pollingInterval,
            ChangeBatchSize = 50
        });

        using var worker = new ConfigurationSynchronizationWorker(
            synchronizer,
            options,
            timeProvider,
            CreateRetryDelayProvider(),
            new ConfigurationSynchronizationState());

        await worker.StartAsync(CancellationToken.None);

        timeProvider.Advance(pollingInterval);
        await client.WaitForChangesRequestAsync();

        timeProvider.Advance(pollingInterval);
        await client.WaitForChangesRequestAsync();

        Assert.Equal(2, client.ChangesRequestCount);

        await worker.StopAsync(CancellationToken.None);
    }

    /// <summary>
    /// Проверяет прекращение периодической синхронизации после остановки worker
    /// </summary>
    [Fact]
    public async Task StopAsyncStopsPeriodicSynchronization()
    {
        var pollingInterval = TimeSpan.FromMinutes(1);
        var client = new StubManagementConfigurationClient(new PublishedSmartLinksSnapshot(7, []));
        var snapshotProvider = new StubConfigurationSnapshotProvider(7);
        var snapshotUpdater = new RecordingConfigurationSnapshotUpdater();
        var synchronizer = new ConfigurationSynchronizer(client, snapshotProvider, snapshotUpdater);
        var timeProvider = new FakeTimeProvider();
        var options = Options.Create(new ConfigurationSynchronizationOptions
        {
            PollingInterval = pollingInterval,
            ChangeBatchSize = 50
        });

        using var worker = new ConfigurationSynchronizationWorker(
            synchronizer,
            options,
            timeProvider,
            CreateRetryDelayProvider(),
            new ConfigurationSynchronizationState());

        await worker.StartAsync(CancellationToken.None);
        await worker.StopAsync(CancellationToken.None);

        timeProvider.Advance(pollingInterval);

        Assert.Equal(0, client.ChangesRequestCount);
    }

    /// <summary>
    /// Создаёт synchronizer с тестовыми зависимостями
    /// </summary>
    private static ConfigurationSynchronizer CreateSynchronizer()
    {
        var client = new StubManagementConfigurationClient(new PublishedSmartLinksSnapshot(0, []));
        var snapshotProvider = new StubConfigurationSnapshotProvider(0);
        var snapshotUpdater = new RecordingConfigurationSnapshotUpdater();

        return new ConfigurationSynchronizer(client, snapshotProvider, snapshotUpdater);
    }

    /// <summary>
    /// Создаёт provider задержки повторной синхронизации для тестов worker
    /// </summary>
    private static ConfigurationSynchronizationRetryDelayProvider CreateRetryDelayProvider()
    {
        return new ConfigurationSynchronizationRetryDelayProvider(
            Options.Create(new ConfigurationSynchronizationOptions()),
            Random.Shared);
    }

    private sealed class StubManagementConfigurationClient : IManagementConfigurationClient
    {
        private readonly PublishedSmartLinksSnapshot _snapshot;
        private readonly IReadOnlyList<ConfigurationChange> _changes;
        private readonly Channel<bool> _changesRequests = Channel.CreateUnbounded<bool>();

        public int SnapshotRequestCount { get; private set; }
        public int ChangesRequestCount { get; private set; }
        public long? ChangesAfterRevision { get; private set; }
        public int? ChangesLimit { get; private set; }

        /// <summary>
        /// Инициализирует клиент возвращаемыми данными
        /// </summary>
        public StubManagementConfigurationClient(PublishedSmartLinksSnapshot snapshot, IReadOnlyList<ConfigurationChange>? changes = null)
        {
            _snapshot = snapshot;
            _changes = changes ?? [];
        }

        /// <summary>
        /// Возвращает подготовленный полный snapshot
        /// </summary>
        public Task<PublishedSmartLinksSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            SnapshotRequestCount++;

            return Task.FromResult(_snapshot);
        }

        /// <summary>
        /// Возвращает подготовленные изменения и запоминает параметры запроса
        /// </summary>
        public Task<IReadOnlyList<ConfigurationChange>> GetChangesAsync(long afterRevision, int limit, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ChangesRequestCount++;
            ChangesAfterRevision = afterRevision;
            ChangesLimit = limit;
            _changesRequests.Writer.TryWrite(true);

            return Task.FromResult(_changes);
        }

        /// <summary>
        /// Ожидает очередной запрос change feed
        /// </summary>
        public async Task WaitForChangesRequestAsync()
        {
            await _changesRequests.Reader
                .ReadAsync()
                .AsTask()
                .WaitAsync(TimeSpan.FromSeconds(1));
        }
    }

    private sealed class StubConfigurationSnapshotProvider : IConfigurationSnapshotProvider
    {
        /// <summary>
        /// Инициализирует provider текущей ревизией
        /// </summary>
        public StubConfigurationSnapshotProvider(long revision)
        {
            Revision = revision;
        }

        public long Revision { get; }

        /// <summary>
        /// Возвращает отсутствие конфигурации для неиспользуемого поиска
        /// </summary>
        public bool TryGetBySlug(string slug, [NotNullWhen(true)] out SmartLinkConfiguration? configuration)
        {
            configuration = null;
            return false;
        }
    }

    private sealed class RecordingConfigurationSnapshotUpdater : IConfigurationSnapshotUpdater
    {
        public PublishedSmartLinksSnapshot? ReplacedSnapshot { get; private set; }
        public IReadOnlyList<ConfigurationChange>? AppliedChanges { get; private set; }

        /// <summary>
        /// Запоминает переданный полный snapshot
        /// </summary>
        public void ReplaceSnapshot(PublishedSmartLinksSnapshot snapshot)
        {
            ReplacedSnapshot = snapshot;
        }

        /// <summary>
        /// Запоминает переданные изменения
        /// </summary>
        public void ApplyChanges(IReadOnlyList<ConfigurationChange> changes)
        {
            AppliedChanges = changes;
        }
    }
}