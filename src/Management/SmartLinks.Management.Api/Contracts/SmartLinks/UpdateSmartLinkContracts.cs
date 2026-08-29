namespace SmartLinks.Management.Api.Contracts.SmartLinks;

/// <summary>
/// Полная замена умной ссылки
/// </summary>
/// <param name="Slug">Новое уникальное значение короткого адреса</param>
/// <param name="DefaultUrl">Новый абсолютный адрес по умолчанию</param>
/// <param name="IsActive">Новый признак активности умной ссылки</param>
/// <param name="Rules">Новый полный набор правил маршрутизации</param>
public sealed record UpdateSmartLinkRequest(
    string Slug,
    string DefaultUrl,
    bool IsActive,
    IReadOnlyCollection<SmartLinkRuleRequest> Rules);