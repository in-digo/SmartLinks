using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartLinks.Management.Infrastructure.Persistence.Publication.Entities;

namespace SmartLinks.Management.Infrastructure.Persistence.Configurations;

internal sealed class ConfigurationChangeEntityTypeConfiguration : IEntityTypeConfiguration<ConfigurationChangeEntity>
{
    /// <summary>
    /// Настраивает хранение журнала опубликованных конфигураций
    /// </summary>
    public void Configure(EntityTypeBuilder<ConfigurationChangeEntity> builder)
    {
        builder.ToTable("configuration_changes", "management");

        builder.HasKey(entity => entity.Revision);

        builder.Property(entity => entity.Revision)
            .HasColumnName("revision")
            .UseIdentityByDefaultColumn();

        builder.Property(entity => entity.SmartLinkId)
            .HasColumnName("smart_link_id")
            .ValueGeneratedNever();

        builder.Property(entity => entity.ConfigurationJson)
            .HasColumnName("configuration")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.HasIndex(entity => entity.SmartLinkId)
            .HasDatabaseName("ix_configuration_changes_smart_link_id");
    }
}