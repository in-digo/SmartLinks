using SmartLinks.Management.Application.SmartLinks.Models;

namespace SmartLinks.Management.Application.SmartLinks.Create;

public sealed record CreateSmartLinkRequest(
    string Slug,
    string DefaultUrl,
    bool IsActive,
    IReadOnlyCollection<SmartLinkRuleInput> Rules);