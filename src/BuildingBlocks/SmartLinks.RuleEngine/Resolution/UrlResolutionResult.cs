namespace SmartLinks.RuleEngine.Resolution;

// Содержит итог выбора целевого URL
public sealed record UrlResolutionResult(
    UrlResolutionStatus Status,
    string? TargetUrl);