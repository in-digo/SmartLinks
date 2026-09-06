using SmartLinks.Contracts.Configurations;
using SmartLinks.Management.Application.Abstractions;
using SmartLinks.Management.Application.Exceptions;
using SmartLinks.RuleEngine.Conditions;

namespace SmartLinks.Management.Application.SmartLinks.Publication;

public sealed class PublishSmartLinkUseCase
{
    private readonly ISmartLinkRepository _repository;
    private readonly IConfigurationChangeLog _changeLog;
    private readonly ConditionDslCompiler _conditionDslCompiler;

    /// <summary>
    /// Инициализирует сценарий публикации умной ссылки
    /// </summary>
    public PublishSmartLinkUseCase(
        ISmartLinkRepository repository,
        IConfigurationChangeLog changeLog,
        ConditionDslCompiler conditionDslCompiler)
    {
        _repository = repository;
        _changeLog = changeLog;
        _conditionDslCompiler = conditionDslCompiler;
    }

    /// <summary>
    /// Проверяет DSL и публикует snapshot конфигурации
    /// </summary>
    public async Task<long> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var smartLink = await _repository.GetByIdAsync(id, cancellationToken);

        if (smartLink is null)
            throw SmartLinkNotFoundException.ForId(id);

        foreach (var rule in smartLink.Rules)
            _conditionDslCompiler.Compile(rule.ConditionDsl);

        var rules = smartLink.Rules
            .Select(rule => new SmartLinkRuleSnapshot(
                rule.Priority,
                rule.IsEnabled,
                rule.TargetUrl,
                rule.ConditionDsl))
            .ToList()
            .AsReadOnly();

        var configuration = new SmartLinkConfigurationSnapshot(
            smartLink.Id,
            smartLink.Slug,
            smartLink.DefaultUrl,
            smartLink.IsActive,
            rules);

        var change = await _changeLog.AppendAsync(configuration, cancellationToken);
        return change.Revision;
    }
}