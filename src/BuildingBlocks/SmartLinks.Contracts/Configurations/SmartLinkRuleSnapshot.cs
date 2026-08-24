namespace SmartLinks.Contracts.Configurations;

public sealed record SmartLinkRuleSnapshot(
    int Priority,
    bool IsEnabled,
    string TargetUrl,
    string ConditionDsl);