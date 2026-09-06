using Microsoft.EntityFrameworkCore;
using SmartLinks.Contracts.Configurations;
using SmartLinks.Management.Infrastructure.Persistence;
using SmartLinks.Management.Infrastructure.Persistence.Publication;
using SmartLinks.Management.IntegrationTests.Infrastructure;

namespace SmartLinks.Management.IntegrationTests.Persistence;

public sealed class PublishedConfigurationReaderTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    public PublishedConfigurationReaderTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Проверяет чтение полного snapshot и его глобальной ревизии
    /// </summary>
    [Fact]
    public async Task GetSnapshotAsyncReturnsPublishedConfigurationsAndGlobalRevision()
    {
        await ResetDatabaseAsync();

        var firstSnapshot = CreateSnapshot(
            slug: "first-link",
            defaultUrl: "https://example.com/first",
            countryCode: "KZ");
        var secondSnapshot = CreateSnapshot(
            slug: "second-link",
            defaultUrl: "https://example.com/second",
            countryCode: "DE");

        await AppendSnapshotAsync(firstSnapshot);
        await AppendSnapshotAsync(secondSnapshot);

        await using var context = CreateContext();
        var reader = new PublishedConfigurationReader(context);

        var result = await reader.GetSnapshotAsync(CancellationToken.None);
        var firstResult = result.Configurations.Single(configuration => configuration.Id == firstSnapshot.Id);
        var secondResult = result.Configurations.Single(configuration => configuration.Id == secondSnapshot.Id);

        Assert.Equal(2, result.Revision);
        Assert.Equal(2, result.Configurations.Count);
        AssertSnapshotsEqual(firstSnapshot, firstResult);
        AssertSnapshotsEqual(secondSnapshot, secondResult);
    }

    /// <summary>
    /// Проверяет пустой snapshot до первой публикации
    /// </summary>
    [Fact]
    public async Task GetSnapshotAsyncWithoutPublicationsReturnsZeroRevisionAndEmptyConfigurations()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();
        var reader = new PublishedConfigurationReader(context);

        var result = await reader.GetSnapshotAsync(CancellationToken.None);

        Assert.Equal(0, result.Revision);
        Assert.Empty(result.Configurations);
    }

    /// <summary>
    /// Проверяет последнее состояние ссылок и high-water revision полного snapshot
    /// </summary>
    [Fact]
    public async Task GetSnapshotAsyncReturnsLatestStatesAtHighWaterRevision()
    {
        await ResetDatabaseAsync();

        var firstId = Guid.NewGuid();
        var firstSnapshot = CreateSnapshot(
            firstId,
            "b-link",
            "https://example.com/version-one",
            "KZ");
        var secondSnapshot = CreateSnapshot(
            slug: "a-link",
            defaultUrl: "https://example.com/second",
            countryCode: "DE");
        var updatedFirstSnapshot = CreateSnapshot(
            firstId,
            "b-link",
            "https://example.com/version-two",
            "JP");

        await AppendSnapshotAsync(firstSnapshot);
        await AppendSnapshotAsync(secondSnapshot);
        await AppendSnapshotAsync(updatedFirstSnapshot);

        await using var context = CreateContext();
        var reader = new PublishedConfigurationReader(context);

        var result = await reader.GetSnapshotAsync(CancellationToken.None);
        var firstResult = result.Configurations.Single(configuration => configuration.Id == firstId);
        var secondResult = result.Configurations.Single(configuration => configuration.Id == secondSnapshot.Id);
        string[] expectedSlugs = ["a-link", "b-link"];

        Assert.Equal(3, result.Revision);
        Assert.Equal(2, result.Configurations.Count);
        Assert.Equal(expectedSlugs, result.Configurations.Select(configuration => configuration.Slug));
        AssertSnapshotsEqual(updatedFirstSnapshot, firstResult);
        AssertSnapshotsEqual(secondSnapshot, secondResult);
    }

    /// <summary>
    /// Проверяет неизменяемость полного snapshot и вложенных правил
    /// </summary>
    [Fact]
    public async Task GetSnapshotAsyncReturnsReadOnlyCollections()
    {
        await ResetDatabaseAsync();

        await AppendSnapshotAsync(CreateSnapshot());

        await using var context = CreateContext();
        var reader = new PublishedConfigurationReader(context);

        var result = await reader.GetSnapshotAsync(CancellationToken.None);
        var configurations = Assert.IsAssignableFrom<IList<SmartLinkConfigurationSnapshot>>(result.Configurations);
        var rules = Assert.IsAssignableFrom<IList<SmartLinkRuleSnapshot>>(result.Configurations[0].Rules);

        Assert.True(configurations.IsReadOnly);
        Assert.True(rules.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => configurations.Add(result.Configurations[0]));
        Assert.Throws<NotSupportedException>(() => rules.Add(result.Configurations[0].Rules[0]));
    }

    /// <summary>
    /// Проверяет прекращение чтения полного snapshot при отменённой операции
    /// </summary>
    [Fact]
    public async Task GetSnapshotAsyncWithCancelledTokenThrowsOperationCanceledException()
    {
        await ResetDatabaseAsync();

        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await using var context = CreateContext();
        var reader = new PublishedConfigurationReader(context);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            reader.GetSnapshotAsync(cancellationTokenSource.Token));
    }

    /// <summary>
    /// Проверяет чтение изменений после указанной ревизии в правильном порядке
    /// </summary>
    [Fact]
    public async Task GetChangesAsyncReturnsOrderedChangesAfterRevision()
    {
        await ResetDatabaseAsync();

        var firstId = Guid.NewGuid();
        var firstSnapshot = CreateSnapshot(
            firstId,
            "first-link",
            "https://example.com/version-one",
            "KZ");
        var secondSnapshot = CreateSnapshot(
            slug: "second-link",
            defaultUrl: "https://example.com/second",
            countryCode: "DE");
        var updatedFirstSnapshot = CreateSnapshot(
            firstId,
            "first-link",
            "https://example.com/version-two",
            "JP");

        await AppendSnapshotAsync(firstSnapshot);
        await AppendSnapshotAsync(secondSnapshot);
        await AppendSnapshotAsync(updatedFirstSnapshot);

        await using var context = CreateContext();
        var reader = new PublishedConfigurationReader(context);

        var changes = await reader.GetChangesAsync(
            afterRevision: 1,
            limit: 10,
            CancellationToken.None);
        var changeList = Assert.IsAssignableFrom<IList<ConfigurationChange>>(changes);
        long[] expectedRevisions = [2, 3];

        Assert.Equal(expectedRevisions, changes.Select(change => change.Revision));
        AssertSnapshotsEqual(secondSnapshot, changes[0].Configuration);
        AssertSnapshotsEqual(updatedFirstSnapshot, changes[1].Configuration);
        Assert.True(changeList.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => changeList.Add(changes[0]));
    }

    /// <summary>
    /// Проверяет ограничение количества возвращаемых изменений
    /// </summary>
    [Fact]
    public async Task GetChangesAsyncAppliesLimit()
    {
        await ResetDatabaseAsync();

        await AppendSnapshotAsync(CreateSnapshot(slug: "first-link"));
        await AppendSnapshotAsync(CreateSnapshot(slug: "second-link"));
        await AppendSnapshotAsync(CreateSnapshot(slug: "third-link"));

        await using var context = CreateContext();
        var reader = new PublishedConfigurationReader(context);

        var changes = await reader.GetChangesAsync(
            afterRevision: 0,
            limit: 2,
            CancellationToken.None);
        long[] expectedRevisions = [1, 2];

        Assert.Equal(expectedRevisions, changes.Select(change => change.Revision));
    }

    /// <summary>
    /// Проверяет пустой change feed после последней ревизии
    /// </summary>
    [Fact]
    public async Task GetChangesAsyncAfterLatestRevisionReturnsEmptyCollection()
    {
        await ResetDatabaseAsync();

        var change = await AppendSnapshotAsync(CreateSnapshot());

        await using var context = CreateContext();
        var reader = new PublishedConfigurationReader(context);

        var changes = await reader.GetChangesAsync(
            change.Revision,
            limit: 10,
            CancellationToken.None);

        Assert.Empty(changes);
    }

    /// <summary>
    /// Проверяет отклонение недопустимых параметров change feed
    /// </summary>
    [Theory]
    [InlineData(-1, 1, "afterRevision")]
    [InlineData(0, 0, "limit")]
    [InlineData(0, -1, "limit")]
    public async Task GetChangesAsyncWithInvalidArgumentsThrowsArgumentOutOfRangeException(
        long afterRevision,
        int limit,
        string parameterName)
    {
        await using var context = CreateContext();
        var reader = new PublishedConfigurationReader(context);

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            reader.GetChangesAsync(afterRevision, limit, CancellationToken.None));

        Assert.Equal(parameterName, exception.ParamName);
    }

    /// <summary>
    /// Проверяет прекращение чтения change feed при отменённой операции
    /// </summary>
    [Fact]
    public async Task GetChangesAsyncWithCancelledTokenThrowsOperationCanceledException()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await using var context = CreateContext();
        var reader = new PublishedConfigurationReader(context);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            reader.GetChangesAsync(0, 10, cancellationTokenSource.Token));
    }

    /// <summary>
    /// Добавляет опубликованный snapshot через новый контекст
    /// </summary>
    private async Task<ConfigurationChange> AppendSnapshotAsync(SmartLinkConfigurationSnapshot snapshot)
    {
        await using var context = CreateContext();
        var changeLog = new ConfigurationChangeLog(context);

        return await changeLog.AppendAsync(snapshot, CancellationToken.None);
    }

    /// <summary>
    /// Очищает и повторно создаёт тестовую базу данных
    /// </summary>
    private async Task ResetDatabaseAsync()
    {
        await using var context = CreateContext();

        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }

    /// <summary>
    /// Создаёт опубликованный snapshot для теста
    /// </summary>
    private static SmartLinkConfigurationSnapshot CreateSnapshot(
        Guid? id = null,
        string slug = "summer-sale",
        string defaultUrl = "https://example.com/default",
        string countryCode = "KZ")
    {
        SmartLinkRuleSnapshot[] rules =
        [
            new(
                10,
                true,
                "https://example.com/target",
                CreateCountryDsl(countryCode))
        ];

        return new SmartLinkConfigurationSnapshot(
            id ?? Guid.NewGuid(),
            slug,
            defaultUrl,
            true,
            rules);
    }

    /// <summary>
    /// Создаёт допустимый DSL проверки страны
    /// </summary>
    private static string CreateCountryDsl(string countryCode)
    {
        return $$"""
            {
              "dslVersion": 1,
              "condition": {
                "type": "country",
                "parameters": {
                  "countryCode": "{{countryCode}}"
                }
              }
            }
            """;
    }

    /// <summary>
    /// Проверяет полное соответствие двух snapshot
    /// </summary>
    private static void AssertSnapshotsEqual(SmartLinkConfigurationSnapshot expected, SmartLinkConfigurationSnapshot actual)
    {
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Slug, actual.Slug);
        Assert.Equal(expected.DefaultUrl, actual.DefaultUrl);
        Assert.Equal(expected.IsActive, actual.IsActive);
        Assert.Equal(expected.Rules.Count, actual.Rules.Count);

        for (var index = 0; index < expected.Rules.Count; index++)
            Assert.Equal(expected.Rules[index], actual.Rules[index]);
    }

    /// <summary>
    /// Создаёт контекст для тестовой базы PostgreSQL
    /// </summary>
    private ManagementDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ManagementDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;

        return new ManagementDbContext(options);
    }
}