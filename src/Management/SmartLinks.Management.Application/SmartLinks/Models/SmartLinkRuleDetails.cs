namespace SmartLinks.Management.Application.SmartLinks.Models;

public sealed record SmartLinkRuleDetails(
    int Priority,
    bool IsEnabled,
    string TargetUrl,
    string ConditionDsl);