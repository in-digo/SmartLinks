using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmartLinks.Contracts.Configurations;
using SmartLinks.Management.Application.Abstractions;
using SmartLinks.Management.Infrastructure.Persistence.Publication.Entities;

namespace SmartLinks.Management.Infrastructure.Persistence.Publication;

public sealed class ConfigurationChangeLog : IConfigurationChangeLog
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly ManagementDbContext _dbContext;

    /// <summary>
    /// Инициализирует PostgreSQL-журнал опубликованных конфигураций
    /// </summary>
    public ConfigurationChangeLog(ManagementDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Добавляет snapshot со следующей глобальной ревизией
    /// </summary>
    public async Task<ConfigurationChange> AppendAsync(SmartLinkConfigurationSnapshot configuration, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var configurationJson = JsonSerializer.Serialize(configuration, _jsonSerializerOptions);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var changeEntity = new ConfigurationChangeEntity
        {
            SmartLinkId = configuration.Id,
            ConfigurationJson = configurationJson
        };

        _dbContext.Set<ConfigurationChangeEntity>().Add(changeEntity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var publishedSmartLinks = _dbContext.Set<PublishedSmartLinkEntity>();
        var publishedSmartLink = await publishedSmartLinks
            .SingleOrDefaultAsync(entity => entity.SmartLinkId == configuration.Id, cancellationToken);

        if (publishedSmartLink is null)
        {
            publishedSmartLinks.Add(new PublishedSmartLinkEntity
            {
                SmartLinkId = configuration.Id,
                Slug = configuration.Slug,
                Revision = changeEntity.Revision,
                ConfigurationJson = configurationJson
            });
        }
        else
            publishedSmartLink.Replace(configuration.Slug, changeEntity.Revision, configurationJson);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new ConfigurationChange(changeEntity.Revision, configuration);
    }
}