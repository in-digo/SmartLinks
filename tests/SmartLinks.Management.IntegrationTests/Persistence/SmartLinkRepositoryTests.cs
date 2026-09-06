using Microsoft.EntityFrameworkCore;
using SmartLinks.Management.Domain.SmartLinks;
using SmartLinks.Management.Infrastructure.Persistence;
using SmartLinks.Management.Infrastructure.Persistence.Repositories;
using SmartLinks.Management.IntegrationTests.Infrastructure;

namespace SmartLinks.Management.IntegrationTests.Persistence;

public sealed class SmartLinkRepositoryTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    /// <summary>
    /// Инициализирует интеграционные тесты репозитория умных ссылок
    /// </summary>
    public SmartLinkRepositoryTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Проверяет сохранение добавленной умной ссылки в PostgreSQL
    /// </summary>
    [Fact]
    public async Task AddAsyncPersistsSmartLink()
    {
        await ResetDatabaseAsync();

        var smartLink = CreateSmartLink(slug: "repository-add");

        await using (var writeContext = CreateContext())
        {
            var repository = new SmartLinkRepository(writeContext);
            await repository.AddAsync(smartLink, CancellationToken.None);
        }

        await using var readContext = CreateContext();
        var persistedSmartLink = await readContext.SmartLinks
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == smartLink.Id);

        Assert.NotNull(persistedSmartLink);
        Assert.Equal(smartLink.Id, persistedSmartLink.Id);
        Assert.Equal(smartLink.Slug, persistedSmartLink.Slug);
        Assert.Equal(smartLink.DefaultUrl, persistedSmartLink.DefaultUrl);
        Assert.Equal(smartLink.IsActive, persistedSmartLink.IsActive);
    }

    /// <summary>
    /// Проверяет чтение умной ссылки вместе с упорядоченными правилами
    /// </summary>
    [Fact]
    public async Task GetByIdAsyncReturnsSmartLinkWithRules()
    {
        await ResetDatabaseAsync();

        var smartLink = CreateSmartLink();
        smartLink.AddRule(
            20,
            false,
            "https://example.com/germany",
            CreateCountryDsl("DE"));
        smartLink.AddRule(
            10,
            true,
            "https://example.com/kazakhstan",
            CreateCountryDsl("KZ"));

        await SeedAsync(smartLink);

        await using var context = CreateContext();
        var repository = new SmartLinkRepository(context);

        var result = await repository.GetByIdAsync(
            smartLink.Id,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(smartLink.Id, result.Id);
        Assert.Equal(smartLink.Slug, result.Slug);
        Assert.Equal(smartLink.DefaultUrl, result.DefaultUrl);
        Assert.Equal(smartLink.IsActive, result.IsActive);

        int[] expectedPriorities = [10, 20];
        bool[] expectedEnabledStates = [true, false];
        string[] expectedTargetUrls =
        [
            "https://example.com/kazakhstan",
            "https://example.com/germany"
        ];
        string[] expectedConditions =
        [
            CreateCountryDsl("KZ"),
            CreateCountryDsl("DE")
        ];

        Assert.Equal(expectedPriorities, result.Rules.Select(rule => rule.Priority));
        Assert.Equal(expectedEnabledStates, result.Rules.Select(rule => rule.IsEnabled));
        Assert.Equal(expectedTargetUrls, result.Rules.Select(rule => rule.TargetUrl));
        Assert.Equal(expectedConditions, result.Rules.Select(rule => rule.ConditionDsl));
    }

    /// <summary>
    /// Проверяет возврат null при чтении отсутствующей умной ссылки
    /// </summary>
    [Fact]
    public async Task GetByIdAsyncWithUnknownIdReturnsNull()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();
        var repository = new SmartLinkRepository(context);

        var result = await repository.GetByIdAsync(
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.Null(result);
    }

    /// <summary>
    /// Проверяет регистронезависимый поиск существующего короткого адреса
    /// </summary>
    [Fact]
    public async Task ExistsBySlugAsyncIgnoresCase()
    {
        await ResetDatabaseAsync();

        var smartLink = CreateSmartLink(slug: "summer-sale");
        await SeedAsync(smartLink);

        await using var context = CreateContext();
        var repository = new SmartLinkRepository(context);

        var result = await repository.ExistsBySlugAsync(
            "SUMMER-SALE",
            excludedSmartLinkId: null,
            CancellationToken.None);

        Assert.True(result);
    }

    /// <summary>
    /// Проверяет отрицательный результат для отсутствующего короткого адреса
    /// </summary>
    [Fact]
    public async Task ExistsBySlugAsyncWithUnknownSlugReturnsFalse()
    {
        await ResetDatabaseAsync();

        await SeedAsync(CreateSmartLink(slug: "summer-sale"));

        await using var context = CreateContext();
        var repository = new SmartLinkRepository(context);

        var result = await repository.ExistsBySlugAsync(
            "winter-sale",
            excludedSmartLinkId: null,
            CancellationToken.None);

        Assert.False(result);
    }

    /// <summary>
    /// Проверяет исключение указанной умной ссылки из поиска короткого адреса
    /// </summary>
    [Fact]
    public async Task ExistsBySlugAsyncExcludesSpecifiedSmartLink()
    {
        await ResetDatabaseAsync();

        var smartLink = CreateSmartLink(slug: "summer-sale");
        await SeedAsync(smartLink);

        await using var context = CreateContext();
        var repository = new SmartLinkRepository(context);

        var result = await repository.ExistsBySlugAsync(
            "SUMMER-SALE",
            smartLink.Id,
            CancellationToken.None);

        Assert.False(result);
    }

    /// <summary>
    /// Проверяет обнаружение другой ссылки при исключении текущего идентификатора
    /// </summary>
    [Fact]
    public async Task ExistsBySlugAsyncStillFindsAnotherSmartLinkWhenCurrentIdIsExcluded()
    {
        await ResetDatabaseAsync();

        var currentSmartLink = CreateSmartLink(slug: "current-link");
        var anotherSmartLink = CreateSmartLink(slug: "reserved-slug");

        await SeedAsync(currentSmartLink, anotherSmartLink);

        await using var context = CreateContext();
        var repository = new SmartLinkRepository(context);

        var result = await repository.ExistsBySlugAsync(
            "RESERVED-SLUG",
            currentSmartLink.Id,
            CancellationToken.None);

        Assert.True(result);
    }

    /// <summary>
    /// Проверяет полную замену конфигурации и удаление прежних правил
    /// </summary>
    [Fact]
    public async Task UpdateAsyncReplacesSmartLinkAndRules()
    {
        await ResetDatabaseAsync();

        var id = Guid.NewGuid();
        var existingSmartLink = CreateSmartLink(
            id,
            "before-update",
            "https://example.com/before",
            false);

        existingSmartLink.AddRule(
            10,
            true,
            "https://example.com/kazakhstan",
            CreateCountryDsl("KZ"));
        existingSmartLink.AddRule(
            20,
            false,
            "https://example.com/germany",
            CreateCountryDsl("DE"));

        await SeedAsync(existingSmartLink);

        var updatedSmartLink = CreateSmartLink(
            id,
            "after-update",
            "https://example.com/after",
            true);

        updatedSmartLink.AddRule(
            30,
            true,
            "https://example.com/japan",
            CreateCountryDsl("JP"));
        updatedSmartLink.AddRule(
            5,
            false,
            "https://example.com/canada",
            CreateCountryDsl("CA"));

        await using (var updateContext = CreateContext())
        {
            var repository = new SmartLinkRepository(updateContext);
            var loadedSmartLink = await repository.GetByIdAsync(
                id,
                CancellationToken.None);

            Assert.NotNull(loadedSmartLink);

            await repository.UpdateAsync(
                updatedSmartLink,
                CancellationToken.None);
        }

        await using var verificationContext = CreateContext();
        var persistedSmartLink = await verificationContext.SmartLinks
            .AsNoTracking()
            .SingleAsync(candidate => candidate.Id == id);

        int[] expectedPriorities = [5, 30];
        bool[] expectedEnabledStates = [false, true];
        string[] expectedTargetUrls =
        [
            "https://example.com/canada",
            "https://example.com/japan"
        ];
        string[] expectedConditions =
        [
            CreateCountryDsl("CA"),
            CreateCountryDsl("JP")
        ];

        Assert.Equal("after-update", persistedSmartLink.Slug);
        Assert.Equal("https://example.com/after", persistedSmartLink.DefaultUrl);
        Assert.True(persistedSmartLink.IsActive);
        Assert.Equal(expectedPriorities, persistedSmartLink.Rules.Select(rule => rule.Priority));
        Assert.Equal(expectedEnabledStates, persistedSmartLink.Rules.Select(rule => rule.IsEnabled));
        Assert.Equal(expectedTargetUrls, persistedSmartLink.Rules.Select(rule => rule.TargetUrl));
        Assert.Equal(expectedConditions, persistedSmartLink.Rules.Select(rule => rule.ConditionDsl));
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
    /// Сохраняет исходные умные ссылки напрямую через EF Core
    /// </summary>
    private async Task SeedAsync(params SmartLink[] smartLinks)
    {
        await using var context = CreateContext();

        context.SmartLinks.AddRange(smartLinks);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Создаёт тестовую умную ссылку
    /// </summary>
    private static SmartLink CreateSmartLink(
        Guid? id = null,
        string slug = "summer-sale",
        string defaultUrl = "https://example.com/default",
        bool isActive = true) =>
        SmartLink.Create(
            id ?? Guid.NewGuid(),
            slug,
            defaultUrl,
            isActive);

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