using SmartLinks.Management.Application.Abstractions;

namespace SmartLinks.Management.Api.Endpoints;

/// <summary>
/// Содержит внутренние HTTP endpoints опубликованных конфигураций
/// </summary>
public static class PublishedConfigurationEndpoints
{
    /// <summary>
    /// Регистрирует внутренние endpoints snapshot и change feed
    /// </summary>
    /// <param name="endpoints">Построитель маршрутов приложения</param>
    /// <returns>Построитель маршрутов приложения</returns>
    public static IEndpointRouteBuilder MapPublishedConfigurationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/internal/configurations/snapshot",
            GetSnapshotAsync);

        endpoints.MapGet(
            "/internal/configurations/changes",
            GetChangesAsync);

        return endpoints;
    }

    /// <summary>
    /// Получить полный snapshot
    /// </summary>
    /// <remarks>
    /// Возвращает текущие опубликованные конфигурации и глобальную high-water revision
    /// </remarks>
    /// <param name="reader">Reader опубликованных конфигураций</param>
    /// <param name="cancellationToken">Токен отмены HTTP-запроса</param>
    /// <returns>Полный snapshot опубликованных конфигураций</returns>
    private static async Task<IResult> GetSnapshotAsync(IPublishedConfigurationReader reader, CancellationToken cancellationToken)
    {
        var snapshot = await reader.GetSnapshotAsync(cancellationToken);

        return Results.Ok(snapshot);
    }

    /// <summary>
    /// Получить изменения конфигураций
    /// </summary>
    /// <remarks>
    /// Возвращает упорядоченные изменения строго после указанной глобальной ревизии
    /// </remarks>
    /// <param name="afterRevision">Ревизия, после которой возвращаются изменения</param>
    /// <param name="limit">Максимальное количество возвращаемых изменений</param>
    /// <param name="reader">Reader опубликованных конфигураций</param>
    /// <param name="cancellationToken">Токен отмены HTTP-запроса</param>
    /// <returns>Последовательные изменения опубликованных конфигураций</returns>
    private static async Task<IResult> GetChangesAsync(
        long afterRevision,
        int limit,
        IPublishedConfigurationReader reader,
        CancellationToken cancellationToken)
    {
        var changes = await reader.GetChangesAsync(
            afterRevision,
            limit,
            cancellationToken);

        return Results.Ok(changes);
    }
}