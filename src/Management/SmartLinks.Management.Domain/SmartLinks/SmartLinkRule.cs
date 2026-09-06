namespace SmartLinks.Management.Domain.SmartLinks;

public sealed class SmartLinkRule
{
    public int Priority { get; }
    public bool IsEnabled { get; }
    public string TargetUrl { get; }
    public string ConditionDsl { get; }

    /// <summary>
    /// Инициализирует правило умной ссылки
    /// </summary>
    internal SmartLinkRule(int priority, bool isEnabled, string targetUrl, string conditionDsl)
    {
        if (!HttpUrlValidator.IsValid(targetUrl))
            throw new ArgumentException(
                "Целевой URL правила должен быть абсолютным HTTP- или HTTPS-адресом",
                nameof(targetUrl));

        if (string.IsNullOrWhiteSpace(conditionDsl))
            throw new ArgumentException("DSL условия правила не может быть пустым", nameof(conditionDsl));

        Priority = priority;
        IsEnabled = isEnabled;
        TargetUrl = targetUrl;
        ConditionDsl = conditionDsl;
    }
}