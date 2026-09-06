using Microsoft.EntityFrameworkCore;
using SmartLinks.Management.Application.Abstractions;
using SmartLinks.Management.Domain.SmartLinks;

namespace SmartLinks.Management.Infrastructure.Persistence.Repositories;

public sealed class SmartLinkRepository : ISmartLinkRepository
{
    private readonly ManagementDbContext _dbContext;

    /// <summary>
    /// Инициализирует PostgreSQL-репозиторий умных ссылок
    /// </summary>
    public SmartLinkRepository(ManagementDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Возвращает умную ссылку по идентификатору
    /// </summary>
    public async Task<SmartLink?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.SmartLinks
            .AsNoTracking()
            .SingleOrDefaultAsync(smartLink => smartLink.Id == id, cancellationToken);
    }

    /// <summary>
    /// Проверяет существование умной ссылки с указанным коротким адресом
    /// </summary>
    public async Task<bool> ExistsBySlugAsync(string slug, Guid? excludedSmartLinkId, CancellationToken cancellationToken)
    {
        var query = _dbContext.SmartLinks
            .AsNoTracking()
            .Where(smartLink => smartLink.Slug == slug);

        if (excludedSmartLinkId is Guid excludedId)
            query = query.Where(smartLink => smartLink.Id != excludedId);

        return await query.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// Добавляет умную ссылку
    /// </summary>
    public async Task AddAsync(SmartLink smartLink, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _dbContext.SmartLinks.Add(smartLink);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Полностью заменяет конфигурацию умной ссылки
    /// </summary>
    public async Task UpdateAsync(SmartLink smartLink, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var existingSmartLink = await _dbContext.SmartLinks
            .SingleAsync(candidate => candidate.Id == smartLink.Id, cancellationToken);

        _dbContext.RemoveRange(existingSmartLink.Rules);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _dbContext.ChangeTracker.Clear();
        _dbContext.SmartLinks.Attach(smartLink);
        _dbContext.Entry(smartLink).State = EntityState.Modified;

        foreach (var rule in smartLink.Rules)
            _dbContext.Entry(rule).State = EntityState.Added;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}