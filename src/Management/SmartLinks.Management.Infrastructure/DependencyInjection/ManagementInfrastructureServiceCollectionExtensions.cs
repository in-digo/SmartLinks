using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartLinks.Management.Application.Abstractions;
using SmartLinks.Management.Infrastructure.Persistence;
using SmartLinks.Management.Infrastructure.Persistence.Publication;
using SmartLinks.Management.Infrastructure.Persistence.Repositories;

namespace SmartLinks.Management.Infrastructure.DependencyInjection;

/// <summary>
/// Содержит регистрацию инфраструктуры Management
/// </summary>
public static class ManagementInfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует PostgreSQL-хранилище Management
    /// </summary>
    public static IServiceCollection AddManagementInfrastructure(this IServiceCollection services, string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Строка подключения не может быть пустой", nameof(connectionString));

        services.AddDbContext<ManagementDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<ISmartLinkRepository, SmartLinkRepository>();
        services.AddScoped<IConfigurationChangeLog, ConfigurationChangeLog>();
        services.AddScoped<IPublishedConfigurationReader, PublishedConfigurationReader>();

        return services;
    }
}