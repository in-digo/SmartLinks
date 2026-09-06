namespace SmartLinks.Management.Application.SmartLinks.Models;

public sealed record SmartLinkDetails(
    Guid Id,
    string Slug,
    string DefaultUrl,
    bool IsActive,
    IReadOnlyList<SmartLinkRuleDetails> Rules);