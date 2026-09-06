using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartLinks.Management.Domain.SmartLinks;

namespace SmartLinks.Management.Infrastructure.Persistence.Configurations;

internal sealed class SmartLinkEntityTypeConfiguration : IEntityTypeConfiguration<SmartLink>
{
    /// <summary>
    /// Настраивает хранение агрегата умной ссылки и его правил
    /// </summary>
    public void Configure(EntityTypeBuilder<SmartLink> builder)
    {
        builder.ToTable("smart_links", "management");

        builder.HasKey(smartLink => smartLink.Id);

        builder.Property(smartLink => smartLink.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(smartLink => smartLink.Slug)
            .HasColumnName("slug")
            .HasColumnType("citext")
            .IsRequired();

        builder.HasIndex(smartLink => smartLink.Slug)
            .IsUnique()
            .HasDatabaseName("ux_smart_links_slug");

        builder.Property(smartLink => smartLink.DefaultUrl)
            .HasColumnName("default_url")
            .IsRequired();

        builder.Property(smartLink => smartLink.IsActive)
            .HasColumnName("is_active");

        builder.OwnsMany(
            smartLink => smartLink.Rules,
            rules =>
            {
                rules.ToTable("smart_link_rules", "management");

                rules.WithOwner()
                    .HasForeignKey("smart_link_id");

                rules.Property<Guid>("smart_link_id")
                    .HasColumnName("smart_link_id");

                rules.HasKey("smart_link_id", nameof(SmartLinkRule.Priority));

                rules.Property(rule => rule.Priority)
                    .HasColumnName("priority")
                    .ValueGeneratedNever();

                rules.Property(rule => rule.IsEnabled)
                    .HasColumnName("is_enabled");

                rules.Property(rule => rule.TargetUrl)
                    .HasColumnName("target_url")
                    .IsRequired();

                rules.Property(rule => rule.ConditionDsl)
                    .HasColumnName("condition_dsl")
                    .IsRequired();
            });

        builder.Navigation(smartLink => smartLink.Rules)
            .HasField("_rules")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}