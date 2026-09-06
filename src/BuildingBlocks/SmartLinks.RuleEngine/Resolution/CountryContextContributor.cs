namespace SmartLinks.RuleEngine.Resolution;

/// <summary>
/// Добавляет в контекст признак страны клиента
/// </summary>
public sealed class CountryContextContributor : IResolutionContextContributor
{
    private readonly IClientLocationResolver _clientLocationResolver;

    /// <summary>
    /// Создаёт contributor с определителем страны по IP-адресу
    /// </summary>
    public CountryContextContributor(IClientLocationResolver clientLocationResolver)
    {
        _clientLocationResolver = clientLocationResolver;
    }

    /// <summary>
    /// Определяет страну клиента и добавляет её в контекст
    /// </summary>
    public void Contribute(
        UrlResolutionContextBuilder builder,
        UrlResolutionRequest request)
    {
        var countryCode = _clientLocationResolver.ResolveCountryCode(request.IpAddress);
        builder.Add(new CountryFeature(countryCode));
    }
}