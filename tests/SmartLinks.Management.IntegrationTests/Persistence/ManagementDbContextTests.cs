using Microsoft.EntityFrameworkCore;
using SmartLinks.Management.Domain.SmartLinks;
using SmartLinks.Management.Infrastructure.Persistence;
using SmartLinks.Management.IntegrationTests.Infrastructure;

namespace SmartLinks.Management.IntegrationTests.Persistence;

public sealed class ManagementDbContextTests : IClassFixture<PostgreSqlFixture>
{
    private const string _conditionDsl = """
        {
          "dslVersion": 1,
          "condition": {
            "type": "country",
            "parameters": {
              "countryCode": "KZ"
            }
          }
        }
        """;

    private readonly PostgreSqlFixture _fixture;

    /// <summary>
    /// Инициализирует тесты контекста с контейнером PostgreSQL
    /// </summary>
    public ManagementDbContextTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Проверяет сохранение и восстановление полного агрегата умной ссылки
    /// </summary>
    [Fact]
    public async Task SaveChangesAsyncPersistsAndRestoresSmartLinkAggregate()
    {
        var smartLink = SmartLink.Create(
            Guid.NewGuid(),
            "summer-sale",
            "https://example.com/default",
            true);

        smartLink.AddRule(20, false, "https://example.com/twenty", _conditionDsl);
        smartLink.AddRule(10, true, "https://example.com/ten", _conditionDsl);

        await using (var writeContext = CreateContext())
        {
            await writeContext.Database.EnsureDeletedAsync();
            await writeContext.Database.MigrateAsync();
            await writeContext.SmartLinks.AddAsync(smartLink);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();

        var restoredSmartLink = await readContext.SmartLinks.SingleAsync(
            item => item.Id == smartLink.Id);

        int[] expectedPriorities = [10, 20];

        Assert.Equal(smartLink.Id, restoredSmartLink.Id);
        Assert.Equal("summer-sale", restoredSmartLink.Slug);
        Assert.Equal("https://example.com/default", restoredSmartLink.DefaultUrl);
        Assert.True(restoredSmartLink.IsActive);
        Assert.Equal(expectedPriorities, restoredSmartLink.Rules.Select(rule => rule.Priority));
        Assert.True(restoredSmartLink.Rules[0].IsEnabled);
        Assert.Equal("https://example.com/ten", restoredSmartLink.Rules[0].TargetUrl);
        Assert.Equal(_conditionDsl, restoredSmartLink.Rules[0].ConditionDsl);
        Assert.False(restoredSmartLink.Rules[1].IsEnabled);
        Assert.Equal("https://example.com/twenty", restoredSmartLink.Rules[1].TargetUrl);
        Assert.Equal(_conditionDsl, restoredSmartLink.Rules[1].ConditionDsl);
    }

    /// <summary>
    /// Проверяет регистронезависимую уникальность короткого адреса
    /// </summary>
    [Fact]
    public async Task SaveChangesAsyncRejectsDuplicateSlugsIgnoringCase()
    {
        await using var context = CreateContext();

        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();

        var firstSmartLink = SmartLink.Create(
            Guid.NewGuid(),
            "summer-sale",
            "https://example.com/first",
            true);
        var secondSmartLink = SmartLink.Create(
            Guid.NewGuid(),
            "SUMMER-SALE",
            "https://example.com/second",
            true);

        context.SmartLinks.AddRange(firstSmartLink, secondSmartLink);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
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