using SmartLinks.Management.Application.Abstractions;
using SmartLinks.Management.Application.Exceptions;
using SmartLinks.Management.Domain.SmartLinks;

namespace SmartLinks.Management.Application.SmartLinks.Update;

public sealed class UpdateSmartLinkUseCase
{
    private readonly ISmartLinkRepository _repository;

    /// <summary>
    /// Инициализирует сценарий обновления умной ссылки
    /// </summary>
    public UpdateSmartLinkUseCase(ISmartLinkRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Полностью заменяет конфигурацию умной ссылки
    /// </summary>
    public async Task ExecuteAsync(UpdateSmartLinkRequest request, CancellationToken cancellationToken)
    {
        var updatedSmartLink = SmartLink.Create(
            request.Id,
            request.Slug,
            request.DefaultUrl,
            request.IsActive);

        foreach (var rule in request.Rules)
        {
            updatedSmartLink.AddRule(
                rule.Priority,
                rule.IsEnabled,
                rule.TargetUrl,
                rule.ConditionDsl);
        }

        var existingSmartLink = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (existingSmartLink is null)
            throw SmartLinkNotFoundException.ForId(request.Id);

        if (await _repository.ExistsBySlugAsync(
                updatedSmartLink.Slug,
                request.Id,
                cancellationToken))
        {
            throw SmartLinkSlugAlreadyExistsException.ForSlug(updatedSmartLink.Slug);
        }

        await _repository.UpdateAsync(updatedSmartLink, cancellationToken);
    }
}