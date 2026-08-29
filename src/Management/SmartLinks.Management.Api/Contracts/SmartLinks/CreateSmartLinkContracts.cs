namespace SmartLinks.Management.Api.Contracts.SmartLinks;

/// <summary>
/// Описывает запрос создания умной ссылки
/// </summary>
public sealed record CreateSmartLinkRequest(
    string Slug,
    string DefaultUrl,
    bool IsActive,
    IReadOnlyCollection<SmartLinkRuleRequest> Rules);

/// <summary>
/// Описывает правило в запросе Management API
/// </summary>
public sealed record SmartLinkRuleRequest(
    int Priority,
    bool IsEnabled,
    string TargetUrl,
    string ConditionDsl);

/// <summary>
/// Описывает результат создания умной ссылки
/// </summary>
public sealed record CreateSmartLinkResponse(Guid Id);