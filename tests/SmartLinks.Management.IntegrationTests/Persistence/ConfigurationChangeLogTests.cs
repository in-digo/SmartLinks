using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmartLinks.Contracts.Configurations;
using SmartLinks.Management.Infrastructure.Persistence;
using SmartLinks.Management.Infrastructure.Persistence.Publication;
using SmartLinks.Management.IntegrationTests.Infrastructure;

namespace SmartLinks.Management.IntegrationTests.Persistence;

public sealed class ConfigurationChangeLogTests : IClassFixture<PostgreSqlFixture>
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly PostgreSqlFixture _fixture;

    /// <summary>
    /// Инициализирует интеграционные тесты журнала опубликованных конфигураций
    /// </summary>
    public ConfigurationChangeLogTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Проверяет сохранение первой ревизии в текущем состоянии и журнале изменений
    /// </summary>
    [Fact]
    public async Task AppendAsyncStoresFirstRevisionInCurrentStateAndChangeLog()
    {
        await ResetDatabaseAsync();

        var snapshot = CreateSnapshot();
        var change = await AppendSnapshotAsync(snapshot);

        Assert.Equal(1, change.Revision);
        Assert.Equal(snapshot, change.Configuration);

        await using var verificationContext = CreateContext();

        var publishedCount = await verificationContext.Database
            .SqlQueryRaw<long>("SELECT COUNT(*) AS \"Value\" FROM management.published_smart_links")
            .SingleAsync();
        var changeCount = await verificationContext.Database
            .SqlQueryRaw<long>("SELECT COUNT(*) AS \"Value\" FROM management.configuration_changes")
            .SingleAsync();
        var publishedRevision = await verificationContext.Database
            .SqlQueryRaw<long>("SELECT revision AS \"Value\" FROM management.published_smart_links")
            .SingleAsync();
        var changeRevision = await verificationContext.Database
            .SqlQueryRaw<long>("SELECT revision AS \"Value\" FROM management.configuration_changes")
            .SingleAsync();

        Assert.Equal(1, publishedCount);
        Assert.Equal(1, changeCount);
        Assert.Equal(1, publishedRevision);
        Assert.Equal(1, changeRevision);
    }

    /// <summary>
    /// Проверяет обновление текущего состояния с сохранением истории публикаций
    /// </summary>
    [Fact]
    public async Task AppendAsyncRepublishingSmartLinkUpdatesCurrentStateAndPreservesHistory()
    {
        await ResetDatabaseAsync();

        var id = Guid.NewGuid();
        var firstSnapshot = CreateSnapshot(
            id,
            defaultUrl: "https://example.com/version-one",
            countryCode: "KZ");
        var secondSnapshot = CreateSnapshot(
            id,
            defaultUrl: "https://example.com/version-two",
            countryCode: "DE");

        var firstChange = await AppendSnapshotAsync(firstSnapshot);
        var secondChange = await AppendSnapshotAsync(secondSnapshot);

        Assert.Equal(1, firstChange.Revision);
        Assert.Equal(2, secondChange.Revision);

        await using var verificationContext = CreateContext();

        var publishedCount = await verificationContext.Database
            .SqlQueryRaw<long>("SELECT COUNT(*) AS \"Value\" FROM management.published_smart_links")
            .SingleAsync();
        var publishedRevision = await verificationContext.Database
            .SqlQueryRaw<long>("SELECT revision AS \"Value\" FROM management.published_smart_links")
            .SingleAsync();
        var currentJson = await verificationContext.Database
            .SqlQueryRaw<string>("SELECT configuration::text AS \"Value\" FROM management.published_smart_links")
            .SingleAsync();
        var historyJson = await verificationContext.Database
            .SqlQueryRaw<string>("SELECT configuration::text AS \"Value\" FROM management.configuration_changes ORDER BY revision")
            .ToListAsync();

        Assert.Equal(1, publishedCount);
        Assert.Equal(2, publishedRevision);
        Assert.Equal(2, historyJson.Count);
        AssertSnapshotsEqual(firstSnapshot, DeserializeSnapshot(historyJson[0]));
        AssertSnapshotsEqual(secondSnapshot, DeserializeSnapshot(historyJson[1]));
        AssertSnapshotsEqual(secondSnapshot, DeserializeSnapshot(currentJson));
    }

    /// <summary>
    /// Проверяет общую последовательность ревизий для разных умных ссылок
    /// </summary>
    [Fact]
    public async Task AppendAsyncPublishingDifferentSmartLinksUsesGlobalRevisionSequence()
    {
        await ResetDatabaseAsync();

        var firstSnapshot = CreateSnapshot(slug: "first-link");
        var secondSnapshot = CreateSnapshot(slug: "second-link");

        var firstChange = await AppendSnapshotAsync(firstSnapshot);
        var secondChange = await AppendSnapshotAsync(secondSnapshot);

        await using var verificationContext = CreateContext();

        var publishedCount = await verificationContext.Database
            .SqlQueryRaw<long>("SELECT COUNT(*) AS \"Value\" FROM management.published_smart_links")
            .SingleAsync();
        var storedRevisions = await verificationContext.Database
            .SqlQueryRaw<long>("SELECT revision AS \"Value\" FROM management.configuration_changes ORDER BY revision")
            .ToListAsync();

        long[] expectedRevisions = [1, 2];

        Assert.Equal(1, firstChange.Revision);
        Assert.Equal(2, secondChange.Revision);
        Assert.Equal(2, publishedCount);
        Assert.Equal(expectedRevisions, storedRevisions);
    }

    /// <summary>
    /// Проверяет уникальность глобальных ревизий при параллельных публикациях
    /// </summary>
    [Fact]
    public async Task AppendAsyncConcurrentPublicationsUseUniqueGlobalRevisions()
    {
        await ResetDatabaseAsync();

        var firstSnapshot = CreateSnapshot(slug: "first-link");
        var secondSnapshot = CreateSnapshot(slug: "second-link");

        var changes = await Task.WhenAll(
            AppendSnapshotAsync(firstSnapshot),
            AppendSnapshotAsync(secondSnapshot));

        var actualRevisions = changes
            .Select(change => change.Revision)
            .OrderBy(revision => revision)
            .ToArray();

        await using var verificationContext = CreateContext();

        var publishedCount = await verificationContext.Database
            .SqlQueryRaw<long>("SELECT COUNT(*) AS \"Value\" FROM management.published_smart_links")
            .SingleAsync();
        var changeCount = await verificationContext.Database
            .SqlQueryRaw<long>("SELECT COUNT(*) AS \"Value\" FROM management.configuration_changes")
            .SingleAsync();

        long[] expectedRevisions = [1, 2];

        Assert.Equal(expectedRevisions, actualRevisions);
        Assert.Equal(2, publishedCount);
        Assert.Equal(2, changeCount);
    }

    /// <summary>
    /// Проверяет откат журнала при конфликте опубликованного короткого адреса
    /// </summary>
    [Fact]
    public async Task AppendAsyncRollsBackChangeWhenPublishedSlugConflicts()
    {
        await ResetDatabaseAsync();

        var firstSnapshot = CreateSnapshot(slug: "shared-slug");
        var conflictingSnapshot = CreateSnapshot(slug: "SHARED-SLUG");

        await AppendSnapshotAsync(firstSnapshot);

        await Assert.ThrowsAsync<DbUpdateException>(() => AppendSnapshotAsync(conflictingSnapshot));

        await using var verificationContext = CreateContext();

        var publishedCount = await verificationContext.Database
            .SqlQueryRaw<long>("SELECT COUNT(*) AS \"Value\" FROM management.published_smart_links")
            .SingleAsync();
        var changeCount = await verificationContext.Database
            .SqlQueryRaw<long>("SELECT COUNT(*) AS \"Value\" FROM management.configuration_changes")
            .SingleAsync();
        var currentJson = await verificationContext.Database
            .SqlQueryRaw<string>("SELECT configuration::text AS \"Value\" FROM management.published_smart_links")
            .SingleAsync();

        Assert.Equal(1, publishedCount);
        Assert.Equal(1, changeCount);
        AssertSnapshotsEqual(firstSnapshot, DeserializeSnapshot(currentJson));
    }

    /// <summary>
    /// Проверяет отсутствие записей при отменённой публикации
    /// </summary>
    [Fact]
    public async Task AppendAsyncWithCancelledTokenStoresNothing()
    {
        await ResetDatabaseAsync();

        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            AppendSnapshotAsync(CreateSnapshot(), cancellationTokenSource.Token));

        await using var verificationContext = CreateContext();

        var publishedCount = await verificationContext.Database
            .SqlQueryRaw<long>("SELECT COUNT(*) AS \"Value\" FROM management.published_smart_links")
            .SingleAsync();
        var changeCount = await verificationContext.Database
            .SqlQueryRaw<long>("SELECT COUNT(*) AS \"Value\" FROM management.configuration_changes")
            .SingleAsync();

        Assert.Equal(0, publishedCount);
        Assert.Equal(0, changeCount);
    }

    /// <summary>
    /// Добавляет snapshot через новый контекст публикации
    /// </summary>
    private async Task<ConfigurationChange> AppendSnapshotAsync(SmartLinkConfigurationSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        var changeLog = new ConfigurationChangeLog(context);

        return await changeLog.AppendAsync(snapshot, cancellationToken);
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
    /// Создаёт snapshot опубликованной конфигурации
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
    /// Восстанавливает snapshot из JSON
    /// </summary>
    private static SmartLinkConfigurationSnapshot DeserializeSnapshot(string json)
    {
        return JsonSerializer.Deserialize<SmartLinkConfigurationSnapshot>(json, _jsonSerializerOptions)
            ?? throw new InvalidOperationException("Не удалось восстановить snapshot конфигурации");
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