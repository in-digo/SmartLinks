using SmartLinks.Management.Application.Abstractions;
using SmartLinks.Management.Application.Exceptions;
using SmartLinks.Management.Domain.SmartLinks;

namespace SmartLinks.Management.Application.SmartLinks.Create;

public sealed class CreateSmartLinkUseCase
{
    private readonly ISmartLinkRepository _repository;

    /// <summary>
    /// Инициализирует сценарий создания умной ссылки
    /// </summary>
    public CreateSmartLinkUseCase(ISmartLinkRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Создаёт и сохраняет умную ссылку
    /// </summary>
    public async Task<Guid> ExecuteAsync(CreateSmartLinkRequest request, CancellationToken cancellationToken)
    {
        var smartLink = SmartLink.Create(
            Guid.NewGuid(),
            request.Slug,
            request.DefaultUrl,
            request.IsActive);

        foreach (var rule in request.Rules)
        {
            smartLink.AddRule(
                rule.Priority,
                rule.IsEnabled,
                rule.TargetUrl,
                rule.ConditionDsl);
        }

        if (await _repository.ExistsBySlugAsync(
                smartLink.Slug,
                excludedSmartLinkId: null,
                cancellationToken))
        {
            throw SmartLinkSlugAlreadyExistsException.ForSlug(smartLink.Slug);
        }

        await _repository.AddAsync(smartLink, cancellationToken);

        return smartLink.Id;
    }
}