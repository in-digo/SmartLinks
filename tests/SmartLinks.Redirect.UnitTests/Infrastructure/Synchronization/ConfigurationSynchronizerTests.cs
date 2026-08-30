using SmartLinks.Contracts.Configurations;
using SmartLinks.Redirect.Application.Configurations;
using SmartLinks.Redirect.Infrastructure.Management;
using SmartLinks.Redirect.Infrastructure.Synchronization;

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
        var synchronizer = new ConfigurationSynchronizer(client, snapshotUpdater);

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
            () => new ConfigurationSynchronizer(client, snapshotUpdater));

        Assert.Equal("managementConfigurationClient", exception.ParamName);
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
            () => new ConfigurationSynchronizer(client, snapshotUpdater));

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
        var synchronizer = new ConfigurationSynchronizer(client, snapshotUpdater);
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
        var synchronizer = new ConfigurationSynchronizer(client, snapshotUpdater);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => synchronizer.LoadSnapshotAsync(CancellationToken.None));

        Assert.Same(expectedException, exception);
        Assert.Null(snapshotUpdater.ReplacedSnapshot);
    }

    private sealed class StubManagementConfigurationClient : IManagementConfigurationClient
    {
        private readonly PublishedSmartLinksSnapshot _snapshot;
        private readonly Exception? _snapshotException;

        /// <summary>
        /// Инициализирует клиент возвращаемым snapshot или ожидаемой ошибкой
        /// </summary>
        public StubManagementConfigurationClient(PublishedSmartLinksSnapshot snapshot, Exception? snapshotException = null)
        {
            _snapshot = snapshot;
            _snapshotException = snapshotException;
        }

        public int SnapshotRequestCount { get; private set; }

        public CancellationToken SnapshotCancellationToken { get; private set; }

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
        /// Отклоняет неиспользуемый в тесте запрос изменений
        /// </summary>
        public Task<IReadOnlyList<ConfigurationChange>> GetChangesAsync(long afterRevision, int limit, CancellationToken cancellationToken)
        {
            throw new NotSupportedException("Получение изменений не используется в этом тесте");
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
        /// Отклоняет неиспользуемое в тесте применение изменений
        /// </summary>
        public void ApplyChanges(IReadOnlyList<ConfigurationChange> changes)
        {
            throw new NotSupportedException("Применение изменений не используется в этом тесте");
        }
    }
}