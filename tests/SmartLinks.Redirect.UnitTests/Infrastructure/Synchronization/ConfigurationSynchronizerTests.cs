using System.Diagnostics.CodeAnalysis;
using SmartLinks.Contracts.Configurations;
using SmartLinks.Redirect.Application.Configurations;
using SmartLinks.Redirect.Infrastructure.Management;
using SmartLinks.Redirect.Infrastructure.Synchronization;
using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.Redirect.UnitTests.Infrastructure.Synchronization;

public sealed class ConfigurationSynchronizerTests
{
    /// <summary>
    /// Проверяет загрузку полного snapshot и передачу его в локальное хранилище
    /// </summary>
    [Fact]
    public async Task LoadSnapshotAsyncGetsSnapshotAndReplacesCurrentSnapshot()
    {
        var expectedSnapshot = new PublishedSmartLinksSnapshot(7, []);
        var client = new StubManagementConfigurationClient(expectedSnapshot);
        var snapshotUpdater = new RecordingConfigurationSnapshotUpdater();
        var synchronizer = new ConfigurationSynchronizer(
            client,
            new StubConfigurationSnapshotProvider(0),
            snapshotUpdater);

        await synchronizer.LoadSnapshotAsync(CancellationToken.None);

        Assert.Equal(1, client.SnapshotRequestCount);
        Assert.Same(expectedSnapshot, snapshotUpdater.ReplacedSnapshot);
    }

    /// <summary>
    /// Проверяет ошибку создания synchronizer без клиента Management API
    /// </summary>
    [Fact]
    public void ConstructorWithNullManagementConfigurationClientThrowsArgumentNullException()
    {
        IManagementConfigurationClient client = null!;
        var snapshotUpdater = new RecordingConfigurationSnapshotUpdater();

        var exception = Assert.Throws<ArgumentNullException>(
            () => new ConfigurationSynchronizer(
                client,
                new StubConfigurationSnapshotProvider(0),
                snapshotUpdater));

        Assert.Equal("managementConfigurationClient", exception.ParamName);
    }

    /// <summary>
    /// Проверяет ошибку создания synchronizer без provider текущего snapshot
    /// </summary>
    [Fact]
    public void ConstructorWithNullSnapshotProviderThrowsArgumentNullException()
    {
        var snapshot = new PublishedSmartLinksSnapshot(1, []);
        var client = new StubManagementConfigurationClient(snapshot);
        IConfigurationSnapshotProvider snapshotProvider = null!;
        var snapshotUpdater = new RecordingConfigurationSnapshotUpdater();

        var exception = Assert.Throws<ArgumentNullException>(
            () => new ConfigurationSynchronizer(client, snapshotProvider, snapshotUpdater));

        Assert.Equal("snapshotProvider", exception.ParamName);
    }

    /// <summary>
    /// Проверяет ошибку создания synchronizer без хранилища snapshot
    /// </summary>
    [Fact]
    public void ConstructorWithNullSnapshotUpdaterThrowsArgumentNullException()
    {
        var snapshot = new PublishedSmartLinksSnapshot(1, []);
        var client = new StubManagementConfigurationClient(snapshot);
        IConfigurationSnapshotUpdater snapshotUpdater = null!;

        var exception = Assert.Throws<ArgumentNullException>(
            () => new ConfigurationSynchronizer(
                client,
                new StubConfigurationSnapshotProvider(0),
                snapshotUpdater));

        Assert.Equal("snapshotUpdater", exception.ParamName);
    }

    /// <summary>
    /// Проверяет передачу токена отмены клиенту Management API
    /// </summary>
    [Fact]
    public async Task LoadSnapshotAsyncPassesCancellationTokenToManagementClient()
    {
        var snapshot = new PublishedSmartLinksSnapshot(1, []);
        var client = new StubManagementConfigurationClient(snapshot);
        var snapshotUpdater = new RecordingConfigurationSnapshotUpdater();
        var synchronizer = new ConfigurationSynchronizer(client, new StubConfigurationSnapshotProvider(0), snapshotUpdater);
        using var cancellationTokenSource = new CancellationTokenSource();

        await synchronizer.LoadSnapshotAsync(cancellationTokenSource.Token);

        Assert.Equal(cancellationTokenSource.Token, client.SnapshotCancellationToken);
    }

    /// <summary>
    /// Проверяет сохранение текущего snapshot при ошибке Management API
    /// </summary>
    [Fact]
    public async Task LoadSnapshotAsyncDoesNotReplaceSnapshotWhenManagementClientFails()
    {
        var snapshot = new PublishedSmartLinksSnapshot(1, []);
        var expectedException = new HttpRequestException("Management API недоступен");
        var client = new StubManagementConfigurationClient(snapshot, expectedException);
        var snapshotUpdater = new RecordingConfigurationSnapshotUpdater();
        var synchronizer = new ConfigurationSynchronizer(client, new StubConfigurationSnapshotProvider(0), snapshotUpdater);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => synchronizer.LoadSnapshotAsync(CancellationToken.None));

        Assert.Same(expectedException, exception);
        Assert.Null(snapshotUpdater.ReplacedSnapshot);
    }

    /// <summary>
    /// Проверяет получение изменений после текущей ревизии и их применение
    /// </summary>
    [Fact]
    public async Task SynchronizeChangesAsyncRequestsChangesAfterCurrentRevisionAndAppliesThem()
    {
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
        var client = new StubManagementConfigurationClient(
            new PublishedSmartLinksSnapshot(7, []),
            changes: expectedChanges);
        var snapshotProvider = new StubConfigurationSnapshotProvider(7);
        var snapshotUpdater = new RecordingConfigurationSnapshotUpdater();
        var synchronizer = new ConfigurationSynchronizer(client, snapshotProvider, snapshotUpdater);

        await synchronizer.SynchronizeChangesAsync(
            limit: 50,
            CancellationToken.None);

        Assert.Equal(1, client.ChangesRequestCount);
        Assert.Equal(7, client.ChangesAfterRevision);
        Assert.Equal(50, client.ChangesLimit);
        Assert.Same(expectedChanges, snapshotUpdater.AppliedChanges);
    }

    /// <summary>
    /// Проверяет отклонение недопустимого лимита до запроса Management API
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task SynchronizeChangesAsyncWithInvalidLimitThrowsArgumentOutOfRangeException(int limit)
    {
        var client = new StubManagementConfigurationClient(new PublishedSmartLinksSnapshot(7, []));
        var snapshotUpdater = new RecordingConfigurationSnapshotUpdater();
        var synchronizer = new ConfigurationSynchronizer(
            client,
            new StubConfigurationSnapshotProvider(7),
            snapshotUpdater);

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => synchronizer.SynchronizeChangesAsync(limit, CancellationToken.None));

        Assert.Equal("limit", exception.ParamName);
        Assert.Equal(0, client.ChangesRequestCount);
        Assert.Null(snapshotUpdater.AppliedChanges);
    }

    /// <summary>
    /// Проверяет передачу токена отмены в запрос change feed
    /// </summary>
    [Fact]
    public async Task SynchronizeChangesAsyncPassesCancellationTokenToManagementClient()
    {
        var client = new StubManagementConfigurationClient(new PublishedSmartLinksSnapshot(7, []));
        var snapshotUpdater = new RecordingConfigurationSnapshotUpdater();
        var synchronizer = new ConfigurationSynchronizer(
            client,
            new StubConfigurationSnapshotProvider(7),
            snapshotUpdater);
        using var cancellationTokenSource = new CancellationTokenSource();

        await synchronizer.SynchronizeChangesAsync(
            limit: 50,
            cancellationTokenSource.Token);

        Assert.Equal(cancellationTokenSource.Token, client.ChangesCancellationToken);
    }

    /// <summary>
    /// Проверяет сохранение текущего snapshot при ошибке получения change feed
    /// </summary>
    [Fact]
    public async Task SynchronizeChangesAsyncDoesNotApplyChangesWhenManagementClientFails()
    {
        var expectedException = new HttpRequestException("Management API недоступен");
        var client = new StubManagementConfigurationClient(
            new PublishedSmartLinksSnapshot(7, []),
            changesException: expectedException);
        var snapshotUpdater = new RecordingConfigurationSnapshotUpdater();
        var synchronizer = new ConfigurationSynchronizer(
            client,
            new StubConfigurationSnapshotProvider(7),
            snapshotUpdater);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => synchronizer.SynchronizeChangesAsync(
                limit: 50,
                CancellationToken.None));

        Assert.Same(expectedException, exception);
        Assert.Null(snapshotUpdater.AppliedChanges);
    }

    private sealed class StubManagementConfigurationClient : IManagementConfigurationClient
    {
        private readonly PublishedSmartLinksSnapshot _snapshot;
        private readonly Exception? _snapshotException;
        private readonly IReadOnlyList<ConfigurationChange> _changes;
        private readonly Exception? _changesException;

        public int SnapshotRequestCount { get; private set; }
        public CancellationToken SnapshotCancellationToken { get; private set; }
        public int ChangesRequestCount { get; private set; }
        public long? ChangesAfterRevision { get; private set; }
        public int? ChangesLimit { get; private set; }
        public CancellationToken ChangesCancellationToken { get; private set; }

        /// <summary>
        /// Инициализирует клиент возвращаемыми данными или ожидаемыми ошибками
        /// </summary>
        public StubManagementConfigurationClient(
            PublishedSmartLinksSnapshot snapshot,
            Exception? snapshotException = null,
            IReadOnlyList<ConfigurationChange>? changes = null,
            Exception? changesException = null)
        {
            _snapshot = snapshot;
            _snapshotException = snapshotException;
            _changes = changes ?? [];
            _changesException = changesException;
        }

        /// <summary>
        /// Возвращает подготовленный snapshot или ожидаемую ошибку
        /// </summary>
        public Task<PublishedSmartLinksSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            SnapshotRequestCount++;
            SnapshotCancellationToken = cancellationToken;

            if (_snapshotException is not null)
                return Task.FromException<PublishedSmartLinksSnapshot>(_snapshotException);

            return Task.FromResult(_snapshot);
        }

        /// <summary>
        /// Возвращает подготовленные изменения и запоминает параметры запроса
        /// </summary>
        public Task<IReadOnlyList<ConfigurationChange>> GetChangesAsync(
            long afterRevision,
            int limit,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ChangesRequestCount++;
            ChangesAfterRevision = afterRevision;
            ChangesLimit = limit;
            ChangesCancellationToken = cancellationToken;

            if (_changesException is not null)
                return Task.FromException<IReadOnlyList<ConfigurationChange>>(_changesException);

            return Task.FromResult(_changes);
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
}