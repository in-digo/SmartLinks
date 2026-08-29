using SmartLinks.Management.Api.Contracts.SmartLinks;
using SmartLinks.Management.Application.SmartLinks.Models;
using SmartLinks.Management.Application.SmartLinks.Queries;
using ApplicationCreateSmartLinkRequest = SmartLinks.Management.Application.SmartLinks.Create.CreateSmartLinkRequest;
using ApplicationUpdateSmartLinkRequest = SmartLinks.Management.Application.SmartLinks.Update.UpdateSmartLinkRequest;
using CreateSmartLinkUseCase = SmartLinks.Management.Application.SmartLinks.Create.CreateSmartLinkUseCase;
using UpdateSmartLinkUseCase = SmartLinks.Management.Application.SmartLinks.Update.UpdateSmartLinkUseCase;

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

        endpoints.MapGet("/api/smart-links/{id:guid}", GetSmartLinkAsync);

        endpoints.MapPut("/api/smart-links/{id:guid}", UpdateSmartLinkAsync)
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

        return Results.Created(
            $"/api/smart-links/{id}",
            new CreateSmartLinkResponse(id));
    }

    /// <summary>
    /// Возвращает умную ссылку по идентификатору
    /// </summary>
    private static async Task<IResult> GetSmartLinkAsync(
        Guid id,
        GetSmartLinkUseCase useCase,
        CancellationToken cancellationToken)
    {
        var smartLink = await useCase.ExecuteAsync(id, cancellationToken);

        var rules = smartLink.Rules
            .Select(rule => new SmartLinkRuleResponse(
                rule.Priority,
                rule.IsEnabled,
                rule.TargetUrl,
                rule.ConditionDsl))
            .ToArray();

        var response = new GetSmartLinkResponse(
            smartLink.Id,
            smartLink.Slug,
            smartLink.DefaultUrl,
            smartLink.IsActive,
            rules);

        return Results.Ok(response);
    }

    /// <summary>
    /// Полностью заменяет конфигурацию умной ссылки
    /// </summary>
    private static async Task<IResult> UpdateSmartLinkAsync(
        Guid id,
        UpdateSmartLinkRequest request,
        UpdateSmartLinkUseCase useCase,
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

        var applicationRequest = new ApplicationUpdateSmartLinkRequest(
            id,
            request.Slug,
            request.DefaultUrl,
            request.IsActive,
            rules);

        await useCase.ExecuteAsync(applicationRequest, cancellationToken);

        return Results.NoContent();
    }
}