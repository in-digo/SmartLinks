namespace SmartLinks.RuleEngine.Resolution;

/// <summary>
/// Содержит определённый тип устройства клиента
/// </summary>
public sealed record DeviceFeature(string? DeviceType) : IResolutionFeature;