using Microsoft.EntityFrameworkCore;
using SmartLinks.Management.Domain.SmartLinks;

namespace SmartLinks.Management.Infrastructure.Persistence;

public sealed class ManagementDbContext : DbContext
{
    public DbSet<SmartLink> SmartLinks => Set<SmartLink>();

    /// <summary>
    /// Инициализирует контекст хранения конфигураций умных ссылок
    /// </summary>
    public ManagementDbContext(DbContextOptions<ManagementDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Применяет EF Core mappings из инфраструктурной сборки
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasPostgresExtension("citext");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ManagementDbContext).Assembly);
    }
}