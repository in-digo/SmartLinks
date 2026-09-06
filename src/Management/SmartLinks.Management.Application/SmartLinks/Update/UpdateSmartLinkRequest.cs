using SmartLinks.Management.Application.SmartLinks.Models;

namespace SmartLinks.Management.Application.SmartLinks.Update;

public sealed record UpdateSmartLinkRequest(
    Guid Id,
    string Slug,
    string DefaultUrl,
    bool IsActive,
    IReadOnlyCollection<SmartLinkRuleInput> Rules);