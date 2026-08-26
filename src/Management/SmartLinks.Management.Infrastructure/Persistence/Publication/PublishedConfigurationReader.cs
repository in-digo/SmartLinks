using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmartLinks.Contracts.Configurations;
using SmartLinks.Management.Application.Abstractions;
using SmartLinks.Management.Infrastructure.Persistence.Publication.Entities;

namespace SmartLinks.Management.Infrastructure.Persistence.Publication;

public sealed class PublishedConfigurationReader : IPublishedConfigurationReader
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly ManagementDbContext _dbContext;

    /// <summary>
    /// Инициализирует PostgreSQL-reader опубликованных конфигураций
    /// </summary>
    public PublishedConfigurationReader(ManagementDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Возвращает полный snapshot опубликованных конфигураций
    /// </summary>
    public async Task<PublishedSmartLinksSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var publishedSmartLinks = await _dbContext.Set<PublishedSmartLinkEntity>()
            .AsNoTracking()
            .OrderBy(entity => entity.Slug)
            .ToListAsync(cancellationToken);

        var configurations = publishedSmartLinks
            .Select(entity => DeserializeSnapshot(entity.ConfigurationJson))
            .ToList()
            .AsReadOnly();

        var revision = publishedSmartLinks.Count == 0 ? 0 : publishedSmartLinks.Max(entity => entity.Revision);

        return new PublishedSmartLinksSnapshot(revision, configurations);
    }

    /// <summary>
    /// Возвращает изменения после указанной глобальной ревизии
    /// </summary>
    public async Task<IReadOnlyList<ConfigurationChange>> GetChangesAsync(long afterRevision, int limit, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (afterRevision < 0)
            throw new ArgumentOutOfRangeException(nameof(afterRevision), afterRevision, "Ревизия не может быть отрицательной");

        if (limit <= 0)
            throw new ArgumentOutOfRangeException(nameof(limit), limit, "Лимит должен быть положительным");

        var changeEntities = await _dbContext.Set<ConfigurationChangeEntity>()
            .AsNoTracking()
            .Where(entity => entity.Revision > afterRevision)
            .OrderBy(entity => entity.Revision)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return changeEntities
            .Select(entity => new ConfigurationChange(entity.Revision, DeserializeSnapshot(entity.ConfigurationJson)))
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Восстанавливает неизменяемый snapshot опубликованной конфигурации
    /// </summary>
    private static SmartLinkConfigurationSnapshot DeserializeSnapshot(string configurationJson)
    {
        var snapshot = JsonSerializer.Deserialize<SmartLinkConfigurationSnapshot>(configurationJson, _jsonSerializerOptions)
            ?? throw new InvalidOperationException("Не удалось восстановить snapshot конфигурации");

        return snapshot with { Rules = snapshot.Rules.ToList().AsReadOnly() };
    }
}