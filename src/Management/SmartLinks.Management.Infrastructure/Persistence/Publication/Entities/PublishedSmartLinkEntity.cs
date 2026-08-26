namespace SmartLinks.Management.Infrastructure.Persistence.Publication.Entities;

internal sealed class PublishedSmartLinkEntity
{
    public Guid SmartLinkId { get; set; }

    public string Slug { get; set; } = string.Empty;

    public long Revision { get; set; }

    public string ConfigurationJson { get; set; } = string.Empty;

    /// <summary>
    /// Заменяет текущее опубликованное состояние новой ревизией
    /// </summary>
    internal void Replace(string slug, long revision, string configurationJson)
    {
        Slug = slug;
        Revision = revision;
        ConfigurationJson = configurationJson;
    }
}