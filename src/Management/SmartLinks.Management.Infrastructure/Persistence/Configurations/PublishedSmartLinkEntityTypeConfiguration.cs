using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartLinks.Management.Infrastructure.Persistence.Publication.Entities;

namespace SmartLinks.Management.Infrastructure.Persistence.Configurations;

internal sealed class PublishedSmartLinkEntityTypeConfiguration : IEntityTypeConfiguration<PublishedSmartLinkEntity>
{
    /// <summary>
    /// Настраивает хранение текущего опубликованного состояния
    /// </summary>
    public void Configure(EntityTypeBuilder<PublishedSmartLinkEntity> builder)
    {
        builder.ToTable("published_smart_links", "management");

        builder.HasKey(entity => entity.SmartLinkId);

        builder.Property(entity => entity.SmartLinkId)
            .HasColumnName("smart_link_id")
            .ValueGeneratedNever();

        builder.Property(entity => entity.Slug)
            .HasColumnName("slug")
            .HasColumnType("citext")
            .IsRequired();

        builder.Property(entity => entity.Revision)
            .HasColumnName("revision")
            .ValueGeneratedNever();

        builder.Property(entity => entity.ConfigurationJson)
            .HasColumnName("configuration")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.HasIndex(entity => entity.Slug)
            .IsUnique()
            .HasDatabaseName("ux_published_smart_links_slug");
    }
}