using System.Text.Json;
using SmartLinks.Contracts.Configurations;
using SmartLinks.Management.Application.Exceptions;
using SmartLinks.Management.Application.SmartLinks.Publication;
using SmartLinks.Management.Domain.SmartLinks;
using SmartLinks.Management.UnitTests.TestDoubles;
using SmartLinks.RuleEngine.Conditions;
using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.Management.UnitTests.Application.SmartLinks.Publication;

public class PublishSmartLinkUseCaseTests
{
    /// <summary>
    /// Проверяет компиляцию всех правил и добавление неизменяемого snapshot
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncWithExistingSmartLinkCompilesRulesAndAppendsSnapshot()
    {
        var repository = new InMemorySmartLinkRepository();
        var smartLink = CreateSmartLink();
        repository.Seed(smartLink);
        var changeLog = new InMemoryConfigurationChangeLog();
        var conditionFactory = new RecordingConditionFactory();
        var useCase = new PublishSmartLinkUseCase(
            repository,
            changeLog,
            CreateCompiler(conditionFactory));

        var revision = await useCase.ExecuteAsync(smartLink.Id, CancellationToken.None);

        var change = Assert.Single(changeLog.Changes);
        var snapshot = change.Configuration;
        int[] expectedPriorities = [10, 20];

        Assert.Equal(1, revision);
        Assert.Equal(revision, change.Revision);
        Assert.Equal(smartLink.Id, snapshot.Id);
        Assert.Equal(smartLink.Slug, snapshot.Slug);
        Assert.Equal(smartLink.DefaultUrl, snapshot.DefaultUrl);
        Assert.Equal(smartLink.IsActive, snapshot.IsActive);
        Assert.Equal(expectedPriorities, snapshot.Rules.Select(rule => rule.Priority));
        Assert.Equal(CreateDsl("ten"), snapshot.Rules[0].ConditionDsl);
        Assert.Equal(CreateDsl("twenty"), snapshot.Rules[1].ConditionDsl);
        Assert.Equal(2, conditionFactory.CreateCallCount);
        Assert.Equal(1, changeLog.AppendCallCount);

        var rules = Assert.IsAssignableFrom<IList<SmartLinkRuleSnapshot>>(snapshot.Rules);

        Assert.True(rules.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => rules.Add(snapshot.Rules[0]));

        smartLink.AddRule(30, true, "https://example.com/thirty", CreateDsl("thirty"));

        Assert.Equal(2, snapshot.Rules.Count);
    }

    /// <summary>
    /// Проверяет публикацию ссылки без правил
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncWithoutRulesAppendsSnapshotWithoutCompilation()
    {
        var repository = new InMemorySmartLinkRepository();
        var smartLink = CreateSmartLink(addRules: false);
        repository.Seed(smartLink);
        var changeLog = new InMemoryConfigurationChangeLog();
        var conditionFactory = new RecordingConditionFactory();
        var useCase = new PublishSmartLinkUseCase(
            repository,
            changeLog,
            CreateCompiler(conditionFactory));

        var revision = await useCase.ExecuteAsync(smartLink.Id, CancellationToken.None);

        Assert.Equal(1, revision);
        Assert.Empty(Assert.Single(changeLog.Changes).Configuration.Rules);
        Assert.Equal(0, conditionFactory.CreateCallCount);
    }

    /// <summary>
    /// Проверяет ошибку публикации отсутствующей умной ссылки
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncWithUnknownIdThrowsSmartLinkNotFoundException()
    {
        var repository = new InMemorySmartLinkRepository();
        var changeLog = new InMemoryConfigurationChangeLog();
        var conditionFactory = new RecordingConditionFactory();
        var useCase = new PublishSmartLinkUseCase(
            repository,
            changeLog,
            CreateCompiler(conditionFactory));
        var id = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<SmartLinkNotFoundException>(() =>
            useCase.ExecuteAsync(id, CancellationToken.None));

        Assert.Equal(id, exception.SmartLinkId);
        Assert.Equal(0, conditionFactory.CreateCallCount);
        Assert.Equal(0, changeLog.AppendCallCount);
    }

    /// <summary>
    /// Проверяет отсутствие записи в журнал при ошибке любого DSL
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncWithInvalidDslDoesNotAppendSnapshot()
    {
        var repository = new InMemorySmartLinkRepository();
        var smartLink = SmartLink.Create(
            Guid.NewGuid(),
            "summer-sale",
            "https://example.com/default",
            true);

        smartLink.AddRule(10, true, "https://example.com/ten", CreateDsl("ten"));
        smartLink.AddRule(
            20,
            true,
            "https://example.com/twenty",
            """
            {
              "dslVersion": 2,
              "condition": {
                "type": "test",
                "parameters": {
                  "value": "twenty"
                }
              }
            }
            """);

        repository.Seed(smartLink);
        var changeLog = new InMemoryConfigurationChangeLog();
        var conditionFactory = new RecordingConditionFactory();
        var useCase = new PublishSmartLinkUseCase(
            repository,
            changeLog,
            CreateCompiler(conditionFactory));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.ExecuteAsync(smartLink.Id, CancellationToken.None));

        Assert.Equal(1, conditionFactory.CreateCallCount);
        Assert.Equal(0, changeLog.AppendCallCount);
        Assert.Empty(changeLog.Changes);
    }

    /// <summary>
    /// Проверяет создание новой ревизии при каждой публикации
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncTwiceReturnsMonotonicallyIncreasingRevisions()
    {
        var repository = new InMemorySmartLinkRepository();
        var smartLink = CreateSmartLink();
        repository.Seed(smartLink);
        var changeLog = new InMemoryConfigurationChangeLog();
        var useCase = new PublishSmartLinkUseCase(
            repository,
            changeLog,
            CreateCompiler(new RecordingConditionFactory()));

        var firstRevision = await useCase.ExecuteAsync(
            smartLink.Id,
            CancellationToken.None);
        var secondRevision = await useCase.ExecuteAsync(
            smartLink.Id,
            CancellationToken.None);

        long[] expectedRevisions = [1, 2];

        Assert.Equal(1, firstRevision);
        Assert.Equal(2, secondRevision);
        Assert.Equal(expectedRevisions, changeLog.Changes.Select(change => change.Revision));
    }

    /// <summary>
    /// Проверяет передачу токена отмены в репозиторий и журнал
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncPassesCancellationTokenToDependencies()
    {
        var repository = new InMemorySmartLinkRepository();
        var smartLink = CreateSmartLink();
        repository.Seed(smartLink);
        var changeLog = new InMemoryConfigurationChangeLog();
        var useCase = new PublishSmartLinkUseCase(
            repository,
            changeLog,
            CreateCompiler(new RecordingConditionFactory()));
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        await useCase.ExecuteAsync(smartLink.Id, cancellationToken);

        Assert.Equal(cancellationToken, repository.LastGetByIdCancellationToken);
        Assert.Equal(cancellationToken, changeLog.LastAppendCancellationToken);
    }

    /// <summary>
    /// Проверяет прекращение публикации при отменённой операции
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncWithCancelledTokenThrowsOperationCanceledException()
    {
        var repository = new InMemorySmartLinkRepository();
        var changeLog = new InMemoryConfigurationChangeLog();
        var conditionFactory = new RecordingConditionFactory();
        var useCase = new PublishSmartLinkUseCase(
            repository,
            changeLog,
            CreateCompiler(conditionFactory));
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            useCase.ExecuteAsync(Guid.NewGuid(), cancellationTokenSource.Token));

        Assert.Equal(0, conditionFactory.CreateCallCount);
        Assert.Equal(0, changeLog.AppendCallCount);
    }

    /// <summary>
    /// Создаёт умную ссылку для публикации
    /// </summary>
    private static SmartLink CreateSmartLink(bool addRules = true)
    {
        var smartLink = SmartLink.Create(
            Guid.NewGuid(),
            "summer-sale",
            "https://example.com/default",
            true);

        if (addRules)
        {
            smartLink.AddRule(
                20,
                false,
                "https://example.com/twenty",
                CreateDsl("twenty"));
            smartLink.AddRule(
                10,
                true,
                "https://example.com/ten",
                CreateDsl("ten"));
        }

        return smartLink;
    }

    /// <summary>
    /// Создаёт допустимый DSL тестового условия
    /// </summary>
    private static string CreateDsl(string value)
    {
        return $$"""
            {
              "dslVersion": 1,
              "condition": {
                "type": "test",
                "parameters": {
                  "value": "{{value}}"
                }
              }
            }
            """;
    }

    /// <summary>
    /// Создаёт DSL-компилятор с тестовой фабрикой
    /// </summary>
    private static ConditionDslCompiler CreateCompiler(IConditionFactory conditionFactory)
    {
        return new ConditionDslCompiler(new ConditionCompiler([conditionFactory]));
    }

    private sealed class RecordingConditionFactory : IConditionFactory
    {
        public string Type => "test";

        public int CreateCallCount { get; private set; }

        /// <summary>
        /// Создаёт тестовое скомпилированное условие
        /// </summary>
        public ICompiledCondition Create(JsonElement parameters)
        {
            CreateCallCount++;
            return new StubCondition();
        }
    }

    private sealed class StubCondition : ICompiledCondition
    {
        /// <summary>
        /// Возвращает успешный результат тестового условия
        /// </summary>
        public bool IsMatch(UrlResolutionContext context) => true;
    }
}