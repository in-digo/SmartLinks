namespace SmartLinks.Management.Infrastructure.Persistence.Publication.Entities;

internal sealed class ConfigurationChangeEntity
{
    public long Revision { get; set; }

    public Guid SmartLinkId { get; set; }

    public string ConfigurationJson { get; set; } = string.Empty;
}