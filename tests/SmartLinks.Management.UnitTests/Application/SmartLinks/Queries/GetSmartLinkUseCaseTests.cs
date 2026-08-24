using SmartLinks.Management.Domain.SmartLinks;
using SmartLinks.Management.Application.Exceptions;
using SmartLinks.Management.Application.SmartLinks.Queries;
using SmartLinks.Management.UnitTests.TestDoubles;

namespace SmartLinks.Management.UnitTests.Application.SmartLinks.Queries;

public class GetSmartLinkUseCaseTests
{
    /// <summary>
    /// Проверяет возврат полной конфигурации умной ссылки
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncWithExistingIdReturnsSmartLinkDetails()
    {
        var repository = new InMemorySmartLinkRepository();
        var smartLink = CreateSmartLink();
        repository.Seed(smartLink);
        var useCase = new GetSmartLinkUseCase(repository);

        var result = await useCase.ExecuteAsync(smartLink.Id, CancellationToken.None);

        int[] expectedPriorities = [10, 20];

        Assert.Equal(smartLink.Id, result.Id);
        Assert.Equal(smartLink.Slug, result.Slug);
        Assert.Equal(smartLink.DefaultUrl, result.DefaultUrl);
        Assert.Equal(smartLink.IsActive, result.IsActive);
        Assert.Equal(expectedPriorities, result.Rules.Select(rule => rule.Priority));
        Assert.True(result.Rules[0].IsEnabled);
        Assert.False(result.Rules[1].IsEnabled);
        Assert.Equal("https://example.com/ten", result.Rules[0].TargetUrl);
        Assert.Equal("""{"dslVersion":1}""", result.Rules[0].ConditionDsl);

        smartLink.AddRule(30, true, "https://example.com/thirty", """{"dslVersion":1}""");

        Assert.Equal(2, result.Rules.Count);
    }

    /// <summary>
    /// Проверяет ошибку при отсутствии умной ссылки
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncWithUnknownIdThrowsSmartLinkNotFoundException()
    {
        var repository = new InMemorySmartLinkRepository();
        var useCase = new GetSmartLinkUseCase(repository);
        var id = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<SmartLinkNotFoundException>(() =>
            useCase.ExecuteAsync(id, CancellationToken.None));

        Assert.Equal(id, exception.SmartLinkId);
        Assert.Equal($"Умная ссылка с идентификатором '{id}' не найдена", exception.Message);
    }

    /// <summary>
    /// Проверяет передачу токена отмены в репозиторий
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncPassesCancellationTokenToRepository()
    {
        var repository = new InMemorySmartLinkRepository();
        var smartLink = CreateSmartLink();
        repository.Seed(smartLink);
        var useCase = new GetSmartLinkUseCase(repository);
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        await useCase.ExecuteAsync(smartLink.Id, cancellationToken);

        Assert.Equal(cancellationToken, repository.LastGetByIdCancellationToken);
    }

    /// <summary>
    /// Проверяет прекращение чтения при отменённой операции
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncWithCancelledTokenThrowsOperationCanceledException()
    {
        var repository = new InMemorySmartLinkRepository();
        var useCase = new GetSmartLinkUseCase(repository);
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            useCase.ExecuteAsync(Guid.NewGuid(), cancellationTokenSource.Token));

        Assert.Equal(1, repository.GetByIdCallCount);
    }

    /// <summary>
    /// Создаёт умную ссылку с двумя правилами
    /// </summary>
    private static SmartLink CreateSmartLink()
    {
        var smartLink = SmartLink.Create(
            Guid.NewGuid(),
            "summer-sale",
            "https://example.com/default",
            true);

        smartLink.AddRule(20, false, "https://example.com/twenty", """{"dslVersion":1}""");
        smartLink.AddRule(10, true, "https://example.com/ten", """{"dslVersion":1}""");

        return smartLink;
    }
}