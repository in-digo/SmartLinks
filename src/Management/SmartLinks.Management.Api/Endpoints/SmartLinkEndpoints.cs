using SmartLinks.Management.Api.Contracts.SmartLinks;
using SmartLinks.Management.Application.SmartLinks.Models;
using SmartLinks.Management.Application.SmartLinks.Publication;
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
    /// <param name="endpoints">Построитель маршрутов приложения</param>
    /// <returns>Построитель маршрутов приложения</returns>
    public static IEndpointRouteBuilder MapSmartLinkEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/smart-links", CreateSmartLinkAsync)
            .RequireAuthorization();

        endpoints.MapGet("/api/smart-links/{id:guid}", GetSmartLinkAsync);

        endpoints.MapPut("/api/smart-links/{id:guid}", UpdateSmartLinkAsync)
            .RequireAuthorization();

        endpoints.MapPost("/api/smart-links/{id:guid}/publish", PublishSmartLinkAsync)
            .RequireAuthorization();

        return endpoints;
    }

    /// <summary>
    /// Создать умную ссылку
    /// </summary>
    /// <remarks>
    /// Создаёт редактируемую конфигурацию умной ссылки и возвращает её ID
    /// </remarks>
    /// <param name="request">Полная конфигурация создаваемой умной ссылки</param>
    /// <param name="useCase">Сценарий создания умной ссылки</param>
    /// <param name="cancellationToken">Токен отмены HTTP-запроса</param>
    /// <returns>Результат создания умной ссылки</returns>
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

        var id = await useCase.ExecuteAsync(
            applicationRequest,
            cancellationToken);

        return Results.Created(
            $"/api/smart-links/{id}",
            new CreateSmartLinkResponse(id));
    }

    /// <summary>
    /// Получить умную ссылку
    /// </summary>
    /// <remarks>
    /// Возвращает текущую редактируемую конфигурацию умной ссылки с правилами
    /// </remarks>
    /// <param name="id">ID умной ссылки</param>
    /// <param name="useCase">Сценарий чтения умной ссылки</param>
    /// <param name="cancellationToken">Токен отмены HTTP-запроса</param>
    /// <returns>Текущая конфигурация умной ссылки</returns>
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
    /// Изменить умную ссылку
    /// </summary>
    /// <remarks>
    /// Полностью заменяет редактируемую конфигурацию и правила умной ссылки
    /// </remarks>
    /// <param name="id">ID изменяемой умной ссылки</param>
    /// <param name="request">Новая полная конфигурация умной ссылки</param>
    /// <param name="useCase">Сценарий изменения умной ссылки</param>
    /// <param name="cancellationToken">Токен отмены HTTP-запроса</param>
    /// <returns>Результат изменения умной ссылки</returns>
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

        await useCase.ExecuteAsync(
            applicationRequest,
            cancellationToken);

        return Results.NoContent();
    }

    /// <summary>
    /// Опубликовать умную ссылку
    /// </summary>
    /// <remarks>
    /// Проверяет JSON DSL и публикует текущую конфигурацию с новой глобальной ревизией
    /// </remarks>
    /// <param name="id">ID публикуемой умной ссылки</param>
    /// <param name="useCase">Сценарий публикации умной ссылки</param>
    /// <param name="cancellationToken">Токен отмены HTTP-запроса</param>
    /// <returns>Новая глобальная ревизия опубликованной конфигурации</returns>
    private static async Task<IResult> PublishSmartLinkAsync(
        Guid id,
        PublishSmartLinkUseCase useCase,
        CancellationToken cancellationToken)
    {
        var revision = await useCase.ExecuteAsync(id, cancellationToken);

        return Results.Ok(new PublishSmartLinkResponse(revision));
    }
}