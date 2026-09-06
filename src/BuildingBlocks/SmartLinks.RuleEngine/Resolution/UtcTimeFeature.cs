namespace SmartLinks.RuleEngine.Resolution;

/// <summary>
/// Содержит текущее UTC-время разрешения URL
/// </summary>
public sealed record UtcTimeFeature(DateTimeOffset UtcNow) : IResolutionFeature;