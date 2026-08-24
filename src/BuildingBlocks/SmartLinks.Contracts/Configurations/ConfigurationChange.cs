namespace SmartLinks.Contracts.Configurations;

public sealed record ConfigurationChange(
    long Revision,
    SmartLinkConfigurationSnapshot Configuration);