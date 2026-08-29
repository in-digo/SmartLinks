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
    public static IEndpointRouteBuilder MapPublishedConfigurationEndpoints(
        this IEndpointRouteBuilder endpoints)
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
    /// Возвращает полный snapshot опубликованных конфигураций
    /// </summary>
    private static async Task<IResult> GetSnapshotAsync(
        IPublishedConfigurationReader reader,
        CancellationToken cancellationToken)
    {
        var snapshot = await reader.GetSnapshotAsync(cancellationToken);

        return Results.Ok(snapshot);
    }

    /// <summary>
    /// Возвращает последовательные изменения после указанной ревизии
    /// </summary>
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