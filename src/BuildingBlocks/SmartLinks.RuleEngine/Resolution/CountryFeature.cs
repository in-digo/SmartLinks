namespace SmartLinks.RuleEngine.Resolution;

/// <summary>
/// Содержит определённый код страны клиента
/// </summary>
public sealed record CountryFeature(string? CountryCode) : IResolutionFeature;