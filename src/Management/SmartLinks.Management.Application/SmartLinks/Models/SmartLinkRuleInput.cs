namespace SmartLinks.Management.Application.SmartLinks.Models;

public sealed record SmartLinkRuleInput(
    int Priority,
    bool IsEnabled,
    string TargetUrl,
    string ConditionDsl);