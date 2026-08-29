using SmartLinks.Management.Api.Contracts.SmartLinks;
using SmartLinks.Management.Application.SmartLinks.Models;
using ApplicationCreateSmartLinkRequest = SmartLinks.Management.Application.SmartLinks.Create.CreateSmartLinkRequest;
using CreateSmartLinkUseCase = SmartLinks.Management.Application.SmartLinks.Create.CreateSmartLinkUseCase;

namespace SmartLinks.Management.Api.Endpoints;

/// <summary>
/// Содержит HTTP endpoints управления умными ссылками
/// </summary>
public static class SmartLinkEndpoints
{
    /// <summary>
    /// Регистрирует HTTP endpoints управления умными ссылками
    /// </summary>
    public static IEndpointRouteBuilder MapSmartLinkEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/smart-links", CreateSmartLinkAsync)
            .RequireAuthorization();

        return endpoints;
    }

    /// <summary>
    /// Создаёт умную ссылку и возвращает её идентификатор
    /// </summary>
    private static async Task<IResult> CreateSmartLinkAsync(
        CreateSmartLinkRequest request,
        CreateSmartLinkUseCase useCase,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request.Rules);

        var rules = request.Rules
            .Select(rule => new SmartLinkRuleInput(
                rule.Priority,
                rule.IsEnabled,
                rule.TargetUrl,
                rule.ConditionDsl))
            .ToArray();

        var applicationRequest = new ApplicationCreateSmartLinkRequest(
            request.Slug,
            request.DefaultUrl,
            request.IsActive,
            rules);

        var id = await useCase.ExecuteAsync(applicationRequest, cancellationToken);

        return Results.Created($"/api/smart-links/{id}", new CreateSmartLinkResponse(id));
    }
}