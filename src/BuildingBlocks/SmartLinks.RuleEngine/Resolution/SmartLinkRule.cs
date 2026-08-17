namespace SmartLinks.RuleEngine.Resolution;

// Описывает одно правило выбора целевого URL
public sealed record SmartLinkRule(
    int Priority,
    bool IsEnabled,
    string TargetUrl,
    ICompiledCondition Condition);