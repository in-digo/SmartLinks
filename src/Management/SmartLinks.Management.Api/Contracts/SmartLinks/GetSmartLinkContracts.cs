namespace SmartLinks.Management.Api.Contracts.SmartLinks;

/// <summary>
/// Описывает результат чтения умной ссылки
/// </summary>
public sealed record GetSmartLinkResponse(
    Guid Id,
    string Slug,
    string DefaultUrl,
    bool IsActive,
    IReadOnlyCollection<SmartLinkRuleResponse> Rules);

/// <summary>
/// Описывает правило в ответе Management API
/// </summary>
public sealed record SmartLinkRuleResponse(
    int Priority,
    bool IsEnabled,
    string TargetUrl,
    string ConditionDsl);