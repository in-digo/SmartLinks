using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.RuleEngine.Conditions;

/// <summary>
/// Проверяет соответствие страны запроса заданному коду страны
/// </summary>
public sealed class CountryCondition : ICompiledCondition
{
    private readonly string _сountryCode;

    /// <summary>
    /// Создаёт условие для заданного кода страны
    /// </summary>
    public CountryCondition(string countryCode)
    {
        _сountryCode = countryCode;
    }

    /// <summary>
    /// Проверяет соответствие страны запроса
    /// </summary>
    public bool IsMatch(UrlResolutionContext context)
    {
        var countryFeature = context.GetRequiredFeature<CountryFeature>();

        return string.Equals( countryFeature.CountryCode, _сountryCode, StringComparison.OrdinalIgnoreCase);
    }
}