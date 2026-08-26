using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SmartLinks.Management.Infrastructure.Persistence;

public sealed class ManagementDbContextFactory : IDesignTimeDbContextFactory<ManagementDbContext>
{
    /// <summary>
    /// Создаёт контекст для команд EF Core
    /// </summary>
    public ManagementDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ManagementDbContext>()
            .UseNpgsql("Host=localhost;Database=smartlinks")
            .Options;

        return new ManagementDbContext(options);
    }
}