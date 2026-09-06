using SmartLinks.Management.Application.Abstractions;
using SmartLinks.Management.Application.Exceptions;
using SmartLinks.Management.Application.SmartLinks.Models;

namespace SmartLinks.Management.Application.SmartLinks.Queries;

public sealed class GetSmartLinkUseCase
{
    private readonly ISmartLinkRepository _repository;

    /// <summary>
    /// Инициализирует сценарий чтения умной ссылки
    /// </summary>
    public GetSmartLinkUseCase(ISmartLinkRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Возвращает конфигурацию умной ссылки
    /// </summary>
    public async Task<SmartLinkDetails> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var smartLink = await _repository.GetByIdAsync(id, cancellationToken);

        if (smartLink is null)
            throw SmartLinkNotFoundException.ForId(id);

        var rules = smartLink.Rules
            .Select(rule => new SmartLinkRuleDetails(
                rule.Priority,
                rule.IsEnabled,
                rule.TargetUrl,
                rule.ConditionDsl))
            .ToList()
            .AsReadOnly();

        return new SmartLinkDetails(
            smartLink.Id,
            smartLink.Slug,
            smartLink.DefaultUrl,
            smartLink.IsActive,
            rules);
    }
}