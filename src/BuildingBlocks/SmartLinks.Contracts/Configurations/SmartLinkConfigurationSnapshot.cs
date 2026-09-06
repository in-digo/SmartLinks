namespace SmartLinks.Contracts.Configurations;

public sealed record SmartLinkConfigurationSnapshot(
    Guid Id,
    string Slug,
    string DefaultUrl,
    bool IsActive,
    IReadOnlyList<SmartLinkRuleSnapshot> Rules);