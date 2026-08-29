namespace SmartLinks.Management.Api.Contracts.SmartLinks;

/// <summary>
/// Результат чтения умной ссылки
/// </summary>
/// <param name="Id">ID умной ссылки</param>
/// <param name="Slug">Уникальный короткий адрес умной ссылки</param>
/// <param name="DefaultUrl">Абсолютный адрес по умолчанию</param>
/// <param name="IsActive">Признак активности умной ссылки</param>
/// <param name="Rules">Правила маршрутизации по возрастанию приоритета</param>
public sealed record GetSmartLinkResponse(
    Guid Id,
    string Slug,
    string DefaultUrl,
    bool IsActive,
    IReadOnlyCollection<SmartLinkRuleResponse> Rules);

/// <summary>
/// Правило в ответе Management API
/// </summary>
/// <param name="Priority">Приоритет правила</param>
/// <param name="IsEnabled">Признак активности правила</param>
/// <param name="TargetUrl">Целевой адрес правила</param>
/// <param name="ConditionDsl">Условие применения правила в формате JSON DSL</param>
public sealed record SmartLinkRuleResponse(
    int Priority,
    bool IsEnabled,
    string TargetUrl,
    string ConditionDsl);