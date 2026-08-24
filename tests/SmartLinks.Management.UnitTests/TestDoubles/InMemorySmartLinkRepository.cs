using SmartLinks.Management.Application.Abstractions;
using SmartLinks.Management.Domain.SmartLinks;

namespace SmartLinks.Management.UnitTests.TestDoubles;

internal sealed class InMemorySmartLinkRepository : ISmartLinkRepository
{
    private readonly Dictionary<Guid, SmartLink> _smartLinks = [];

    public SmartLink? AddedSmartLink { get; private set; }

    public int AddCallCount { get; private set; }

    public int ExistsBySlugCallCount { get; private set; }

    public CancellationToken LastAddCancellationToken { get; private set; }

    public CancellationToken LastExistsBySlugCancellationToken { get; private set; }

    /// <summary>
    /// Добавляет исходную умную ссылку в тестовый репозиторий
    /// </summary>
    internal void Seed(SmartLink smartLink)
    {
        _smartLinks.Add(smartLink.Id, smartLink);
    }

    /// <summary>
    /// Возвращает умную ссылку по идентификатору
    /// </summary>
    public Task<SmartLink?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _smartLinks.TryGetValue(id, out var smartLink);
        return Task.FromResult(smartLink);
    }

    /// <summary>
    /// Проверяет существование умной ссылки с указанным коротким адресом
    /// </summary>
    public Task<bool> ExistsBySlugAsync(
        string slug,
        Guid? excludedSmartLinkId,
        CancellationToken cancellationToken)
    {
        ExistsBySlugCallCount++;
        LastExistsBySlugCancellationToken = cancellationToken;
        cancellationToken.ThrowIfCancellationRequested();

        var exists = _smartLinks.Values.Any(smartLink =>
            (!excludedSmartLinkId.HasValue || smartLink.Id != excludedSmartLinkId.Value) &&
            string.Equals(smartLink.Slug, slug, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(exists);
    }

    /// <summary>
    /// Добавляет умную ссылку
    /// </summary>
    public Task AddAsync(SmartLink smartLink, CancellationToken cancellationToken)
    {
        AddCallCount++;
        LastAddCancellationToken = cancellationToken;
        cancellationToken.ThrowIfCancellationRequested();

        _smartLinks.Add(smartLink.Id, smartLink);
        AddedSmartLink = smartLink;

        return Task.CompletedTask;
    }

    /// <summary>
    /// Обновляет умную ссылку
    /// </summary>
    public Task UpdateAsync(SmartLink smartLink, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _smartLinks[smartLink.Id] = smartLink;
        return Task.CompletedTask;
    }
}