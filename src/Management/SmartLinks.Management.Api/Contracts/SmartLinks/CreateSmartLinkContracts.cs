namespace SmartLinks.Management.Api.Contracts.SmartLinks;

/// <summary>
/// Создание умной ссылки
/// </summary>
/// <param name="Slug">Уникальный короткий адрес умной ссылки</param>
/// <param name="DefaultUrl">Абсолютный HTTP- или HTTPS-адрес по умолчанию</param>
/// <param name="IsActive">Признак активности умной ссылки</param>
/// <param name="Rules">Полный набор правил маршрутизации</param>
public sealed record CreateSmartLinkRequest(
    string Slug,
    string DefaultUrl,
    bool IsActive,
    IReadOnlyCollection<SmartLinkRuleRequest> Rules);

/// <summary>
/// Правило в запросе Management API
/// </summary>
/// <param name="Priority">Уникальный приоритет правила внутри ссылки</param>
/// <param name="IsEnabled">Признак участия правила в выборе адреса</param>
/// <param name="TargetUrl">Целевой HTTP- или HTTPS-адрес правила</param>
/// <param name="ConditionDsl">Условие правила в формате JSON DSL версии 1</param>
public sealed record SmartLinkRuleRequest(
    int Priority,
    bool IsEnabled,
    string TargetUrl,
    string ConditionDsl);

/// <summary>
/// Результат создания умной ссылки
/// </summary>
/// <param name="Id">ID умной ссылки</param>
public sealed record CreateSmartLinkResponse(Guid Id);