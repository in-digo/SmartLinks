using SmartLinks.Management.Domain.SmartLinks;

namespace SmartLinks.Management.Application.Abstractions;

public interface ISmartLinkRepository
{
    /// <summary>
    /// Возвращает умную ссылку по идентификатору
    /// </summary>
    Task<SmartLink?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Проверяет существование умной ссылки с указанным коротким адресом
    /// </summary>
    Task<bool> ExistsBySlugAsync(
        string slug,
        Guid? excludedSmartLinkId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Добавляет умную ссылку
    /// </summary>
    Task AddAsync(SmartLink smartLink, CancellationToken cancellationToken);

    /// <summary>
    /// Обновляет умную ссылку
    /// </summary>
    Task UpdateAsync(SmartLink smartLink, CancellationToken cancellationToken);
}