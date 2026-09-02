using SmartLinks.Redirect.Application.Configurations;
using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.Redirect.Api.Middleware;

/// <summary>
/// Обрабатывает переходы по опубликованным умным ссылкам
/// </summary>
public sealed class SmartLinkRedirectMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Создаёт middleware с последующим обработчиком HTTP-конвейера
    /// </summary>
    public SmartLinkRedirectMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Разрешает целевой URL для slug или передаёт запрос следующему обработчику
    /// </summary>
    public async Task InvokeAsync(
        HttpContext context,
        IConfigurationSnapshotProvider snapshotProvider,
        UrlResolutionContextFactory resolutionContextFactory,
        ISmartLinkResolver smartLinkResolver)
    {
        if (!TryGetSlug(context.Request, out var slug) ||
            !snapshotProvider.TryGetBySlug(slug, out var configuration))
        {
            await _next(context);
            return;
        }

        var request = new UrlResolutionRequest(context.Connection.RemoteIpAddress, context.Request.Headers.UserAgent.ToString());
        var resolutionContext = resolutionContextFactory.Create(request);
        var resolutionResult = smartLinkResolver.Resolve(configuration, resolutionContext);

        if (resolutionResult.Status != UrlResolutionStatus.Resolved ||
            resolutionResult.TargetUrl is null)
        {
            await _next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status302Found;
        context.Response.Headers.Location = resolutionResult.TargetUrl;
        context.Response.Headers.CacheControl = "no-store";
    }

    /// <summary>
    /// Извлекает slug из поддерживаемого публичного GET-запроса
    /// </summary>
    private static bool TryGetSlug(HttpRequest request, out string slug)
    {
        slug = request.Path.Value?.Trim('/') ?? string.Empty;

        return HttpMethods.IsGet(request.Method) &&
            !request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase) &&
            !request.Path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(slug) &&
            !slug.Contains('/');
    }
}