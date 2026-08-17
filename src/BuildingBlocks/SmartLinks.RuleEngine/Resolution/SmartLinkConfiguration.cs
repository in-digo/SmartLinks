namespace SmartLinks.RuleEngine.Resolution;

// Содержит конфигурацию умной ссылки, необходимую для выбора URL
public sealed record SmartLinkConfiguration(
    bool IsActive,
    string DefaultUrl,
    IReadOnlyCollection<SmartLinkRule> Rules);