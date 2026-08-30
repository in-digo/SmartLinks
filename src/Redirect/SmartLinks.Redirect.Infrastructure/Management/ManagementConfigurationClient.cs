using System.Net.Http.Json;
using SmartLinks.Contracts.Configurations;

namespace SmartLinks.Redirect.Infrastructure.Management;

/// <summary>
/// Получает опубликованные конфигурации из Management API
/// </summary>
public sealed class ManagementConfigurationClient
{
    private const string _snapshotPath = "/internal/configurations/snapshot";

    private readonly HttpClient _httpClient;

    /// <summary>
    /// Инициализирует клиент HTTP-подключением к Management API
    /// </summary>
    public ManagementConfigurationClient(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        if (httpClient.BaseAddress is null)
            throw new InvalidOperationException("Не задан базовый адрес Management API");

        _httpClient = httpClient;
    }

    /// <summary>
    /// Получает полный snapshot опубликованных конфигураций
    /// </summary>
    public async Task<PublishedSmartLinksSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        return await _httpClient.GetFromJsonAsync<PublishedSmartLinksSnapshot>(
            _snapshotPath,
            cancellationToken)
            ?? throw new InvalidOperationException("Management API вернул пустой snapshot");
    }
}