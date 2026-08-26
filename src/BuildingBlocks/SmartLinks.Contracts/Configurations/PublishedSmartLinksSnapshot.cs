namespace SmartLinks.Contracts.Configurations;

public sealed record PublishedSmartLinksSnapshot(
    long Revision,
    IReadOnlyList<SmartLinkConfigurationSnapshot> Configurations);