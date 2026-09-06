using SmartLinks.Management.Application.Exceptions;
using SmartLinks.Management.Application.SmartLinks.Create;
using SmartLinks.Management.Application.SmartLinks.Models;
using SmartLinks.Management.Domain.SmartLinks;
using SmartLinks.Management.UnitTests.TestDoubles;

namespace SmartLinks.Management.UnitTests.Application.SmartLinks.Create;

public class CreateSmartLinkUseCaseTests
{
    /// <summary>
    /// Проверяет создание и сохранение умной ссылки со всеми правилами
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncWithValidRequestAddsSmartLinkAndReturnsId()
    {
        var repository = new InMemorySmartLinkRepository();
        var useCase = new CreateSmartLinkUseCase(repository);
        SmartLinkRuleInput[] rules =
        [
            new(20, false, "https://example.com/twenty", """{"dslVersion":1}"""),
            new(10, true, "https://example.com/ten", """{"dslVersion":1}""")
        ];
        var request = CreateRequest(rules: rules);

        var id = await useCase.ExecuteAsync(request, CancellationToken.None);

        var smartLink = Assert.IsType<SmartLink>(repository.AddedSmartLink);
        int[] expectedPriorities = [10, 20];

        Assert.NotEqual(Guid.Empty, id);
        Assert.Equal(id, smartLink.Id);
        Assert.Equal(request.Slug, smartLink.Slug);
        Assert.Equal(request.DefaultUrl, smartLink.DefaultUrl);
        Assert.Equal(request.IsActive, smartLink.IsActive);
        Assert.Equal(expectedPriorities, smartLink.Rules.Select(rule => rule.Priority));
        Assert.True(smartLink.Rules[0].IsEnabled);
        Assert.False(smartLink.Rules[1].IsEnabled);
        Assert.Equal(1, repository.ExistsBySlugCallCount);
        Assert.Equal(1, repository.AddCallCount);
    }

    /// <summary>
    /// Проверяет создание умной ссылки без правил
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncWithoutRulesAddsSmartLink()
    {
        var repository = new InMemorySmartLinkRepository();
        var useCase = new CreateSmartLinkUseCase(repository);
        var request = CreateRequest(rules: Array.Empty<SmartLinkRuleInput>());

        await useCase.ExecuteAsync(request, CancellationToken.None);

        Assert.Empty(Assert.IsType<SmartLink>(repository.AddedSmartLink).Rules);
    }

    /// <summary>
    /// Проверяет запрет создания ссылки с уже существующим коротким адресом
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncWithExistingSlugThrowsSmartLinkSlugAlreadyExistsException()
    {
        var repository = new InMemorySmartLinkRepository();
        repository.Seed(
            SmartLink.Create(
                Guid.NewGuid(),
                "Summer-Sale",
                "https://example.com/existing",
                true));

        var useCase = new CreateSmartLinkUseCase(repository);
        var request = CreateRequest(slug: "summer-sale");

        var exception = await Assert.ThrowsAsync<SmartLinkSlugAlreadyExistsException>(() =>
            useCase.ExecuteAsync(request, CancellationToken.None));

        Assert.Equal(request.Slug, exception.Slug);
        Assert.Equal("Умная ссылка с коротким адресом 'summer-sale' уже существует", exception.Message);
        Assert.Equal(0, repository.AddCallCount);
    }

    /// <summary>
    /// Проверяет выполнение доменной валидации до обращения к репозиторию
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncWithInvalidConfigurationDoesNotCallRepository()
    {
        var repository = new InMemorySmartLinkRepository();
        var useCase = new CreateSmartLinkUseCase(repository);
        var request = CreateRequest(slug: " ");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            useCase.ExecuteAsync(request, CancellationToken.None));

        Assert.Equal(0, repository.ExistsBySlugCallCount);
        Assert.Equal(0, repository.AddCallCount);
    }

    /// <summary>
    /// Проверяет валидацию всех правил до обращения к репозиторию
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncWithDuplicateRulePriorityDoesNotCallRepository()
    {
        var repository = new InMemorySmartLinkRepository();
        var useCase = new CreateSmartLinkUseCase(repository);
        SmartLinkRuleInput[] rules =
        [
            new(10, true, "https://example.com/first", """{"dslVersion":1}"""),
            new(10, true, "https://example.com/second", """{"dslVersion":1}""")
        ];
        var request = CreateRequest(rules: rules);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            useCase.ExecuteAsync(request, CancellationToken.None));

        Assert.Equal(0, repository.ExistsBySlugCallCount);
        Assert.Equal(0, repository.AddCallCount);
    }

    /// <summary>
    /// Проверяет передачу токена отмены всем операциям репозитория
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncPassesCancellationTokenToRepository()
    {
        var repository = new InMemorySmartLinkRepository();
        var useCase = new CreateSmartLinkUseCase(repository);
        var request = CreateRequest();
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        await useCase.ExecuteAsync(request, cancellationToken);

        Assert.Equal(cancellationToken, repository.LastExistsBySlugCancellationToken);
        Assert.Equal(cancellationToken, repository.LastAddCancellationToken);
    }

    /// <summary>
    /// Проверяет прекращение создания при отменённой операции
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncWithCancelledTokenThrowsOperationCanceledException()
    {
        var repository = new InMemorySmartLinkRepository();
        var useCase = new CreateSmartLinkUseCase(repository);
        var request = CreateRequest();
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            useCase.ExecuteAsync(request, cancellationTokenSource.Token));

        Assert.Equal(1, repository.ExistsBySlugCallCount);
        Assert.Equal(0, repository.AddCallCount);
    }

    /// <summary>
    /// Создаёт запрос с допустимой конфигурацией
    /// </summary>
    private static CreateSmartLinkRequest CreateRequest(
        string slug = "summer-sale",
        IReadOnlyCollection<SmartLinkRuleInput>? rules = null)
    {
        rules ??=
        [
            new SmartLinkRuleInput(
                10,
                true,
                "https://example.com/target",
                """{"dslVersion":1}""")
        ];

        return new CreateSmartLinkRequest(
            slug,
            "https://example.com/default",
            true,
            rules);
    }
}