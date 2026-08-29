namespace SmartLinks.Management.Api.Contracts.SmartLinks;

/// <summary>
/// Описывает запрос полной замены умной ссылки
/// </summary>
public sealed record UpdateSmartLinkRequest(
    string Slug,
    string DefaultUrl,
    bool IsActive,
    IReadOnlyCollection<SmartLinkRuleRequest> Rules);