namespace SmartLinks.RuleEngine.Resolution;

/// <summary>
/// Добавляет в контекст тип устройства клиента
/// </summary>
public sealed class DeviceContextContributor : IResolutionContextContributor
{
    private readonly IClientDeviceResolver _clientDeviceResolver;

    /// <summary>
    /// Создаёт contributor с определителем типа устройства
    /// </summary>
    public DeviceContextContributor(IClientDeviceResolver clientDeviceResolver)
    {
        _clientDeviceResolver = clientDeviceResolver;
    }

    /// <summary>
    /// Определяет тип устройства и добавляет его в контекст
    /// </summary>
    public void Contribute(UrlResolutionContextBuilder builder, UrlResolutionRequest request)
    {
        var deviceType = _clientDeviceResolver.ResolveDeviceType(request.UserAgent);

        builder.Add(new DeviceFeature(deviceType));
    }
}