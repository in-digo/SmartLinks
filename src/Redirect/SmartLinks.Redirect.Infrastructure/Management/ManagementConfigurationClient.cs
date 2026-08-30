using System.Net.Http.Json;
using SmartLinks.Contracts.Configurations;

namespace SmartLinks.Redirect.Infrastructure.Management;

/// <summary>
/// Получает опубликованные конфигурации из Management API
/// </summary>
public sealed class ManagementConfigurationClient
{
    private const string _snapshotPath = "/internal/configurations/snapshot";
    private const string _changesPath = "/internal/configurations/changes";

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

    /// <summary>
    /// Получает изменения опубликованных конфигураций после указанной ревизии
    /// </summary>
    public async Task<IReadOnlyList<ConfigurationChange>> GetChangesAsync(
        long afterRevision,
        int limit,
        CancellationToken cancellationToken)
    {
        if (afterRevision < 0)
            throw new ArgumentOutOfRangeException(nameof(afterRevision), afterRevision, "Ревизия не может быть отрицательной");

        if (limit <= 0)
            throw new ArgumentOutOfRangeException(nameof(limit), limit, "Лимит должен быть положительным");

        var requestPath = FormattableString.Invariant($"{_changesPath}?afterRevision={afterRevision}&limit={limit}");

        return await _httpClient.GetFromJsonAsync<ConfigurationChange[]>(
            requestPath,
            cancellationToken)
            ?? throw new InvalidOperationException("Management API вернул пустой список изменений");
    }
}