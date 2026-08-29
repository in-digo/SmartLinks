using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace SmartLinks.Management.Api.Authentication;

/// <summary>
/// Аутентифицирует запросы Management API по API-ключу
/// </summary>
public sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private const string _apiKeyConfigurationPath = "Authentication:ApiKey";
    private const string _apiKeyHeaderName = "X-Api-Key";
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Инициализирует обработчик аутентификации по API-ключу
    /// </summary>
    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration) : base(options, logger, encoder)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Проверяет API-ключ и создаёт аутентифицированного пользователя
    /// </summary>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(_apiKeyHeaderName, out var suppliedApiKey))
            return Task.FromResult(AuthenticateResult.NoResult());

        var configuredApiKey = _configuration[_apiKeyConfigurationPath];

        if (string.IsNullOrWhiteSpace(configuredApiKey))
            return Task.FromResult(AuthenticateResult.Fail("API-ключ Management API не настроен"));

        if (!ApiKeysMatch(configuredApiKey, suppliedApiKey.ToString()))
            return Task.FromResult(AuthenticateResult.Fail("Передан неверный API-ключ"));

        Claim[] claims = [new Claim(ClaimTypes.NameIdentifier, "management-api-client")];
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    /// <summary>
    /// Сравнивает API-ключи по хешам за постоянное время
    /// </summary>
    private static bool ApiKeysMatch(string configuredApiKey, string suppliedApiKey)
    {
        var configuredHash = SHA256.HashData(Encoding.UTF8.GetBytes(configuredApiKey));
        var suppliedHash = SHA256.HashData(Encoding.UTF8.GetBytes(suppliedApiKey));

        return CryptographicOperations.FixedTimeEquals(configuredHash, suppliedHash);
    }
}