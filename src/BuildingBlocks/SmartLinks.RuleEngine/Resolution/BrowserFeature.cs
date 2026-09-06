namespace SmartLinks.RuleEngine.Resolution;

/// <summary>
/// Содержит определённый браузер клиента
/// </summary>
public sealed record BrowserFeature(string? Browser) : IResolutionFeature;