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

public sealed class ConfigurationSynchronizationWorkerRetryTests
{
    /// <summary>
    /// Проверяет повторную загрузку первоначального snapshot после ошибки Management API
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncRetriesInitialSnapshotAfterManagementFailure()
    {
        var snapshot = new PublishedSmartLinksSnapshot(7, []);
        var client = new FailingOnceSnapshotManagementConfigurationClient(snapshot);
        var snapshotStore = new StubConfigurationSnapshotStore();
        var synchronizer = new ConfigurationSynchronizer(client, snapshotStore, snapshotStore);
        var timeProvider = new RecordingFakeTimeProvider();
        var options = Options.Create(new ConfigurationSynchronizationOptions
        {
            PollingInterval = TimeSpan.FromHours(1),
            InitialRetryDelay = TimeSpan.FromSeconds(2),
            MaximumRetryDelay = TimeSpan.FromSeconds(2)
        });
        var retryDelayProvider = new ConfigurationSynchronizationRetryDelayProvider(options, new FixedRandom(0.5));
        var synchronizationState = new ConfigurationSynchronizationState();

        using var worker = new ConfigurationSynchronizationWorker(
            synchronizer,
            options,
            timeProvider,
            retryDelayProvider,
            synchronizationState);
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await worker.StartAsync(CancellationToken.None);
        await client.WaitForSnapshotRequestAsync(cancellationTokenSource.Token);

        var retryDelay = await timeProvider.WaitForCreatedTimerAsync(cancellationTokenSource.Token);
        Assert.Equal(TimeSpan.FromSeconds(1), retryDelay);
        Assert.False(synchronizationState.IsReady);

        timeProvider.Advance(retryDelay);

        await client.WaitForSnapshotRequestAsync(cancellationTokenSource.Token);

        var pollingDelay = await timeProvider.WaitForCreatedTimerAsync(cancellationTokenSource.Token);
        Assert.Equal(TimeSpan.FromHours(1), pollingDelay);
        Assert.Equal(2, client.SnapshotRequestCount);
        Assert.Same(snapshot, snapshotStore.ReplacedSnapshot);
        Assert.True(synchronizationState.IsReady);

        await worker.StopAsync(CancellationToken.None);
    }

    /// <summary>
    /// Проверяет ошибку создания worker без состояния синхронизации
    /// </summary>
    [Fact]
    public void ConstructorWithNullSynchronizationStateThrowsArgumentNullException()
    {
        var snapshot = new PublishedSmartLinksSnapshot(7, []);
        var client = new FailingOnceSnapshotManagementConfigurationClient(snapshot);
        var snapshotStore = new StubConfigurationSnapshotStore();
        var synchronizer = new ConfigurationSynchronizer(client, snapshotStore, snapshotStore);
        var options = Options.Create(new ConfigurationSynchronizationOptions());
        var timeProvider = new FakeTimeProvider();
        var retryDelayProvider = new ConfigurationSynchronizationRetryDelayProvider(options, Random.Shared);
        ConfigurationSynchronizationState synchronizationState = null!;

        var exception = Assert.Throws<ArgumentNullException>(() => new ConfigurationSynchronizationWorker(
            synchronizer,
            options,
            timeProvider,
            retryDelayProvider,
            synchronizationState));

        Assert.Equal("synchronizationState", exception.ParamName);
    }

    /// <summary>
    /// Проверяет повторное получение change feed после ошибки Management API
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncRetriesChangeFeedAfterManagementFailure()
    {
        var snapshot = new PublishedSmartLinksSnapshot(7, []);
        var client = new FailingOnceChangesManagementConfigurationClient(snapshot);
        var snapshotStore = new StubConfigurationSnapshotStore();
        var synchronizer = new ConfigurationSynchronizer(client, snapshotStore, snapshotStore);
        var timeProvider = new RecordingFakeTimeProvider();
        var options = Options.Create(new ConfigurationSynchronizationOptions
        {
            PollingInterval = TimeSpan.FromMinutes(1),
            InitialRetryDelay = TimeSpan.FromSeconds(2),
            MaximumRetryDelay = TimeSpan.FromSeconds(2)
        });
        var retryDelayProvider = new ConfigurationSynchronizationRetryDelayProvider(options, new FixedRandom(0.5));

        using var worker = new ConfigurationSynchronizationWorker(synchronizer, options, timeProvider, retryDelayProvider, new ConfigurationSynchronizationState());
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await worker.StartAsync(cancellationTokenSource.Token);

        var pollingDelay = await timeProvider.WaitForCreatedTimerAsync(cancellationTokenSource.Token);
        Assert.Equal(TimeSpan.FromMinutes(1), pollingDelay);

        timeProvider.Advance(pollingDelay);

        await client.WaitForChangesRequestAsync(cancellationTokenSource.Token);

        var retryDelay = await timeProvider.WaitForCreatedTimerAsync(cancellationTokenSource.Token);
        Assert.Equal(TimeSpan.FromSeconds(1), retryDelay);

        timeProvider.Advance(retryDelay);

        await client.WaitForChangesRequestAsync(cancellationTokenSource.Token);

        Assert.Equal(2, client.ChangesRequestCount);

        await worker.StopAsync(CancellationToken.None);
    }

    /// <summary>
    /// Проверяет ошибку создания worker без provider задержки повторной синхронизации
    /// </summary>
    [Fact]
    public void ConstructorWithNullRetryDelayProviderThrowsArgumentNullException()
    {
        var snapshot = new PublishedSmartLinksSnapshot(7, []);
        var client = new FailingOnceSnapshotManagementConfigurationClient(snapshot);
        var snapshotStore = new StubConfigurationSnapshotStore();
        var synchronizer = new ConfigurationSynchronizer(client, snapshotStore, snapshotStore);
        var options = Options.Create(new ConfigurationSynchronizationOptions());
        var timeProvider = new FakeTimeProvider();
        ConfigurationSynchronizationRetryDelayProvider retryDelayProvider = null!;

        var exception = Assert.Throws<ArgumentNullException>(() => new ConfigurationSynchronizationWorker(
            synchronizer,
            options,
            timeProvider,
            retryDelayProvider,
            new ConfigurationSynchronizationState()));

        Assert.Equal("retryDelayProvider", exception.ParamName);
    }

    /// <summary>
    /// Проверяет отмену ожидания повторной загрузки snapshot при остановке worker
    /// </summary>
    [Fact]
    public async Task StopAsyncCancelsInitialSnapshotRetryDelay()
    {
        var snapshot = new PublishedSmartLinksSnapshot(7, []);
        var client = new FailingOnceSnapshotManagementConfigurationClient(snapshot);
        var snapshotStore = new StubConfigurationSnapshotStore();
        var synchronizer = new ConfigurationSynchronizer(client, snapshotStore, snapshotStore);
        var timeProvider = new RecordingFakeTimeProvider();
        var options = Options.Create(new ConfigurationSynchronizationOptions
        {
            PollingInterval = TimeSpan.FromHours(1),
            InitialRetryDelay = TimeSpan.FromSeconds(2),
            MaximumRetryDelay = TimeSpan.FromSeconds(2)
        });
        var retryDelayProvider = new ConfigurationSynchronizationRetryDelayProvider(options, new FixedRandom(0.5));

        using var worker = new ConfigurationSynchronizationWorker(synchronizer, options, timeProvider, retryDelayProvider, new ConfigurationSynchronizationState());
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await worker.StartAsync(CancellationToken.None);
        await client.WaitForSnapshotRequestAsync(cancellationTokenSource.Token);

        var retryDelay = await timeProvider.WaitForCreatedTimerAsync(cancellationTokenSource.Token);
        Assert.Equal(TimeSpan.FromSeconds(1), retryDelay);

        await worker.StopAsync(cancellationTokenSource.Token);

        timeProvider.Advance(retryDelay);

        Assert.Equal(1, client.SnapshotRequestCount);
        Assert.Null(snapshotStore.ReplacedSnapshot);
    }

    /// <summary>
    /// Проверяет отметку готовности после успешной первоначальной загрузки snapshot
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncMarksSynchronizationStateReadyAfterInitialSnapshotLoaded()
    {
        var snapshot = new PublishedSmartLinksSnapshot(7, []);
        var client = new FailingOnceChangesManagementConfigurationClient(snapshot);
        var snapshotStore = new StubConfigurationSnapshotStore();
        var synchronizer = new ConfigurationSynchronizer(client, snapshotStore, snapshotStore);
        var timeProvider = new RecordingFakeTimeProvider();
        var options = Options.Create(new ConfigurationSynchronizationOptions
        {
            PollingInterval = TimeSpan.FromHours(1)
        });
        var retryDelayProvider = new ConfigurationSynchronizationRetryDelayProvider(options, new FixedRandom(0.5));
        var synchronizationState = new ConfigurationSynchronizationState();

        using var worker = new ConfigurationSynchronizationWorker(
            synchronizer,
            options,
            timeProvider,
            retryDelayProvider,
            synchronizationState);
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        Assert.False(synchronizationState.IsReady);

        await worker.StartAsync(CancellationToken.None);

        var pollingDelay = await timeProvider.WaitForCreatedTimerAsync(cancellationTokenSource.Token);
        Assert.Equal(TimeSpan.FromHours(1), pollingDelay);
        Assert.True(synchronizationState.IsReady);

        await worker.StopAsync(cancellationTokenSource.Token);
    }

    private sealed class RecordingFakeTimeProvider : FakeTimeProvider
    {
        private readonly Channel<TimeSpan> _createdTimers = Channel.CreateUnbounded<TimeSpan>();

        /// <summary>
        /// Создаёт таймер и запоминает его первоначальную задержку
        /// </summary>
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = base.CreateTimer(callback, state, dueTime, period);
            _createdTimers.Writer.TryWrite(dueTime);

            return timer;
        }

        /// <summary>
        /// Ожидает создания очередного таймера
        /// </summary>
        public async Task<TimeSpan> WaitForCreatedTimerAsync(CancellationToken cancellationToken)
        {
            return await _createdTimers.Reader.ReadAsync(cancellationToken);
        }
    }

    private sealed class FixedRandom : Random
    {
        private readonly double _value;

        /// <summary>
        /// Инициализирует генератор фиксированным значением
        /// </summary>
        public FixedRandom(double value)
        {
            _value = value;
        }

        /// <summary>
        /// Возвращает фиксированное значение для расчёта jitter
        /// </summary>
        protected override double Sample()
        {
            return _value;
        }
    }

    private sealed class FailingOnceSnapshotManagementConfigurationClient : IManagementConfigurationClient
    {
        private readonly PublishedSmartLinksSnapshot _snapshot;
        private readonly Channel<bool> _snapshotRequests = Channel.CreateUnbounded<bool>();

        /// <summary>
        /// Инициализирует клиент snapshot, возвращаемым после первой ошибки
        /// </summary>
        public FailingOnceSnapshotManagementConfigurationClient(PublishedSmartLinksSnapshot snapshot)
        {
            _snapshot = snapshot;
        }

        public int SnapshotRequestCount { get; private set; }

        /// <summary>
        /// Возвращает ошибку при первом запросе и snapshot при повторном
        /// </summary>
        public Task<PublishedSmartLinksSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            SnapshotRequestCount++;
            _snapshotRequests.Writer.TryWrite(true);

            if (SnapshotRequestCount == 1)
                return Task.FromException<PublishedSmartLinksSnapshot>(new HttpRequestException("Management API недоступен"));

            return Task.FromResult(_snapshot);
        }

        /// <summary>
        /// Возвращает отсутствие изменений
        /// </summary>
        public Task<IReadOnlyList<ConfigurationChange>> GetChangesAsync(long afterRevision, int limit, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult<IReadOnlyList<ConfigurationChange>>([]);
        }

        /// <summary>
        /// Ожидает очередного запроса полного snapshot
        /// </summary>
        public async Task WaitForSnapshotRequestAsync(CancellationToken cancellationToken)
        {
            await _snapshotRequests.Reader.ReadAsync(cancellationToken);
        }
    }

    private sealed class FailingOnceChangesManagementConfigurationClient : IManagementConfigurationClient
    {
        private readonly PublishedSmartLinksSnapshot _snapshot;
        private readonly Channel<bool> _changesRequests = Channel.CreateUnbounded<bool>();

        /// <summary>
        /// Инициализирует клиент snapshot для последующей синхронизации изменений
        /// </summary>
        public FailingOnceChangesManagementConfigurationClient(PublishedSmartLinksSnapshot snapshot)
        {
            _snapshot = snapshot;
        }

        public int ChangesRequestCount { get; private set; }

        /// <summary>
        /// Возвращает первоначальный snapshot
        /// </summary>
        public Task<PublishedSmartLinksSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(_snapshot);
        }

        /// <summary>
        /// Возвращает ошибку при первом запросе и пустой change feed при повторном
        /// </summary>
        public Task<IReadOnlyList<ConfigurationChange>> GetChangesAsync(long afterRevision, int limit, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ChangesRequestCount++;
            _changesRequests.Writer.TryWrite(true);

            if (ChangesRequestCount == 1)
                return Task.FromException<IReadOnlyList<ConfigurationChange>>(new HttpRequestException("Management API недоступен"));

            return Task.FromResult<IReadOnlyList<ConfigurationChange>>([]);
        }

        /// <summary>
        /// Ожидает очередного запроса change feed
        /// </summary>
        public async Task WaitForChangesRequestAsync(CancellationToken cancellationToken)
        {
            await _changesRequests.Reader.ReadAsync(cancellationToken);
        }
    }

    private sealed class StubConfigurationSnapshotStore : IConfigurationSnapshotProvider, IConfigurationSnapshotUpdater
    {
        public long Revision => ReplacedSnapshot?.Revision ?? 0;

        public PublishedSmartLinksSnapshot? ReplacedSnapshot { get; private set; }

        /// <summary>
        /// Возвращает отсутствие конфигурации для неиспользуемого поиска
        /// </summary>
        public bool TryGetBySlug(string slug, [NotNullWhen(true)] out SmartLinkConfiguration? configuration)
        {
            configuration = null;
            return false;
        }

        /// <summary>
        /// Запоминает загруженный полный snapshot
        /// </summary>
        public void ReplaceSnapshot(PublishedSmartLinksSnapshot snapshot)
        {
            ReplacedSnapshot = snapshot;
        }

        /// <summary>
        /// Игнорирует неиспользуемые изменения
        /// </summary>
        public void ApplyChanges(IReadOnlyList<ConfigurationChange> changes)
        {
        }
    }
}