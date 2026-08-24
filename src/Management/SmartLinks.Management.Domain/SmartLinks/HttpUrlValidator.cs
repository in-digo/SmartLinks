namespace SmartLinks.Management.Domain.SmartLinks;

internal static class HttpUrlValidator
{
    /// <summary>
    /// Проверяет абсолютный HTTP- или HTTPS-адрес
    /// </summary>
    internal static bool IsValid(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}