using SmartLinks.Management.Application.Exceptions;
using SmartLinks.Management.Application.SmartLinks.Models;
using SmartLinks.Management.Application.SmartLinks.Update;
using SmartLinks.Management.Domain.SmartLinks;
using SmartLinks.Management.UnitTests.TestDoubles;

namespace SmartLinks.Management.UnitTests.Application.SmartLinks.Update;

public class UpdateSmartLinkUseCaseTests
{
    /// <summary>
    /// Проверяет полную замену конфигурации умной ссылки
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncWithValidRequestUpdatesSmartLink()
    {
        var repository = new InMemorySmartLinkRepository();
        var existingSmartLink = CreateStoredSmartLink();
        repository.Seed(existingSmartLink);
        var useCase = new UpdateSmartLinkUseCase(repository);
        SmartLinkRuleInput[] rules =
        [
            new(30, false, "https://example.com/thirty", """{"dslVersion":1}"""),
            new(10, true, "https://example.com/ten", """{"dslVersion":1}""")
        ];
        var request = CreateRequest(existingSmartLink.Id, rules: rules);

        await useCase.ExecuteAsync(request, CancellationToken.None);

        var updatedSmartLink = Assert.IsType<SmartLink>(repository.UpdatedSmartLink);
        int[] expectedPriorities = [10, 30];

        Assert.Equal(existingSmartLink.Id, updatedSmartLink.Id);
        Assert.Equal(request.Slug, updatedSmartLink.Slug);
        Assert.Equal(request.DefaultUrl, updatedSmartLink.DefaultUrl);
        Assert.Equal(request.IsActive, updatedSmartLink.IsActive);
        Assert.Equal(expectedPriorities, updatedSmartLink.Rules.Select(rule => rule.Priority));
        Assert.Equal("https://example.com/ten", updatedSmartLink.Rules[0].TargetUrl);
        Assert.Equal("https://example.com/thirty", updatedSmartLink.Rules[1].TargetUrl);
        Assert.Equal(existingSmartLink.Id, repository.LastExcludedSmartLinkId);
        Assert.Equal(1, repository.GetByIdCallCount);
        Assert.Equal(1, repository.ExistsBySlugCallCount);
        Assert.Equal(1, repository.UpdateCallCount);
    }

    /// <summary>
    /// Проверяет возможность сохранить собственный короткий адрес
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncWithOwnSlugUpdatesSmartLink()
    {
        var repository = new InMemorySmartLinkRepository();
        var existingSmartLink = CreateStoredSmartLink(slug: "Summer-Sale");
        repository.Seed(existingSmartLink);
        var useCase = new UpdateSmartLinkUseCase(repository);
        var request = CreateRequest(existingSmartLink.Id, slug: "summer-sale");

        await useCase.ExecuteAsync(request, CancellationToken.None);

        Assert.Equal(1, repository.UpdateCallCount);
    }

    /// <summary>
    /// Проверяет запрет короткого адреса, занятого другой умной ссылкой
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncWithExistingSlugThrowsSmartLinkSlugAlreadyExistsException()
    {
        var repository = new InMemorySmartLinkRepository();
        var existingSmartLink = CreateStoredSmartLink(slug: "current");
        var conflictingSmartLink = CreateStoredSmartLink(slug: "Occupied");
        repository.Seed(existingSmartLink);
        repository.Seed(conflictingSmartLink);
        var useCase = new UpdateSmartLinkUseCase(repository);
        var request = CreateRequest(existingSmartLink.Id, slug: "occupied");

        var exception = await Assert.ThrowsAsync<SmartLinkSlugAlreadyExistsException>(() =>
            useCase.ExecuteAsync(request, CancellationToken.None));

        Assert.Equal(request.Slug, exception.Slug);
        Assert.Equal(0, repository.UpdateCallCount);
    }

    /// <summary>
    /// Проверяет ошибку при отсутствии изменяемой умной ссылки
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncWithUnknownIdThrowsSmartLinkNotFoundException()
    {
        var repository = new InMemorySmartLinkRepository();
        var useCase = new UpdateSmartLinkUseCase(repository);
        var id = Guid.NewGuid();
        var request = CreateRequest(id);

        var exception = await Assert.ThrowsAsync<SmartLinkNotFoundException>(() =>
            useCase.ExecuteAsync(request, CancellationToken.None));

        Assert.Equal(id, exception.SmartLinkId);
        Assert.Equal(0, repository.ExistsBySlugCallCount);
        Assert.Equal(0, repository.UpdateCallCount);
    }

    /// <summary>
    /// Проверяет доменную валидацию до обращения к репозиторию
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncWithInvalidConfigurationDoesNotCallRepository()
    {
        var repository = new InMemorySmartLinkRepository();
        var useCase = new UpdateSmartLinkUseCase(repository);
        var request = CreateRequest(Guid.NewGuid(), slug: " ");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            useCase.ExecuteAsync(request, CancellationToken.None));

        Assert.Equal(0, repository.GetByIdCallCount);
        Assert.Equal(0, repository.ExistsBySlugCallCount);
        Assert.Equal(0, repository.UpdateCallCount);
    }

    /// <summary>
    /// Проверяет валидацию всех правил до обращения к репозиторию
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncWithDuplicateRulePriorityDoesNotCallRepository()
    {
        var repository = new InMemorySmartLinkRepository();
        var useCase = new UpdateSmartLinkUseCase(repository);
        SmartLinkRuleInput[] rules =
        [
            new(10, true, "https://example.com/first", """{"dslVersion":1}"""),
            new(10, true, "https://example.com/second", """{"dslVersion":1}""")
        ];
        var request = CreateRequest(Guid.NewGuid(), rules: rules);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            useCase.ExecuteAsync(request, CancellationToken.None));

        Assert.Equal(0, repository.GetByIdCallCount);
        Assert.Equal(0, repository.ExistsBySlugCallCount);
        Assert.Equal(0, repository.UpdateCallCount);
    }

    /// <summary>
    /// Проверяет передачу токена отмены всем операциям репозитория
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncPassesCancellationTokenToRepository()
    {
        var repository = new InMemorySmartLinkRepository();
        var existingSmartLink = CreateStoredSmartLink();
        repository.Seed(existingSmartLink);
        var useCase = new UpdateSmartLinkUseCase(repository);
        var request = CreateRequest(existingSmartLink.Id);
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        await useCase.ExecuteAsync(request, cancellationToken);

        Assert.Equal(cancellationToken, repository.LastGetByIdCancellationToken);
        Assert.Equal(cancellationToken, repository.LastExistsBySlugCancellationToken);
        Assert.Equal(cancellationToken, repository.LastUpdateCancellationToken);
    }

    /// <summary>
    /// Проверяет прекращение обновления при отменённой операции
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncWithCancelledTokenThrowsOperationCanceledException()
    {
        var repository = new InMemorySmartLinkRepository();
        var useCase = new UpdateSmartLinkUseCase(repository);
        var request = CreateRequest(Guid.NewGuid());
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            useCase.ExecuteAsync(request, cancellationTokenSource.Token));

        Assert.Equal(1, repository.GetByIdCallCount);
        Assert.Equal(0, repository.ExistsBySlugCallCount);
        Assert.Equal(0, repository.UpdateCallCount);
    }

    /// <summary>
    /// Создаёт сохранённую умную ссылку
    /// </summary>
    private static SmartLink CreateStoredSmartLink(string slug = "current")
    {
        var smartLink = SmartLink.Create(
            Guid.NewGuid(),
            slug,
            "https://example.com/current-default",
            true);

        smartLink.AddRule(5, true, "https://example.com/old", """{"dslVersion":1}""");

        return smartLink;
    }

    /// <summary>
    /// Создаёт запрос на обновление умной ссылки
    /// </summary>
    private static UpdateSmartLinkRequest CreateRequest(
        Guid id,
        string slug = "updated",
        IReadOnlyCollection<SmartLinkRuleInput>? rules = null)
    {
        rules ??=
        [
            new SmartLinkRuleInput(
                10,
                true,
                "https://example.com/updated",
                """{"dslVersion":1}""")
        ];

        return new UpdateSmartLinkRequest(
            id,
            slug,
            "https://example.com/updated-default",
            false,
            rules);
    }
}