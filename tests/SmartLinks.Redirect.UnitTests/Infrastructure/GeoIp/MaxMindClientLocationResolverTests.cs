using System.Net;
using SmartLinks.Redirect.Infrastructure.GeoIp;

namespace SmartLinks.Redirect.UnitTests.Infrastructure.GeoIp;

public sealed class MaxMindClientLocationResolverTests
{
    /// <summary>
    /// Проверяет определение ISO-кода страны для известного IP-адреса
    /// </summary>
    [Fact]
    public void ResolveCountryCodeReturnsIsoCodeForKnownIpAddress()
    {
        using var resolver = CreateResolver();

        var countryCode = resolver.ResolveCountryCode(IPAddress.Parse("81.2.69.160"));

        Assert.Equal("GB", countryCode);
    }

    /// <summary>
    /// Проверяет отсутствие страны, если IP-адрес не передан
    /// </summary>
    [Fact]
    public void ResolveCountryCodeWithNullIpAddressReturnsNull()
    {
        using var resolver = CreateResolver();

        var countryCode = resolver.ResolveCountryCode(null);

        Assert.Null(countryCode);
    }

    /// <summary>
    /// Проверяет отсутствие страны для неизвестного IP-адреса
    /// </summary>
    [Fact]
    public void ResolveCountryCodeWithUnknownIpAddressReturnsNull()
    {
        using var resolver = CreateResolver();

        var countryCode = resolver.ResolveCountryCode(IPAddress.Parse("10.10.10.10"));

        Assert.Null(countryCode);
    }

    /// <summary>
    /// Проверяет определение страны для IPv4-адреса в IPv6-формате
    /// </summary>
    [Fact]
    public void ResolveCountryCodeMapsIpv4MappedIpv6Address()
    {
        using var resolver = CreateResolver();

        var countryCode = resolver.ResolveCountryCode(IPAddress.Parse("::ffff:81.2.69.160"));

        Assert.Equal("GB", countryCode);
    }

    /// <summary>
    /// Проверяет отклонение отсутствующего пути к базе MaxMind
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void ConstructorWithMissingDatabasePathThrowsArgumentException(string? databasePath)
    {
        var exception = Assert.ThrowsAny<ArgumentException>(
            () => new MaxMindClientLocationResolver(databasePath!));

        Assert.Equal("databasePath", exception.ParamName);
    }

    /// <summary>
    /// Создаёт определитель страны с тестовой базой MaxMind
    /// </summary>
    private static MaxMindClientLocationResolver CreateResolver()
    {
        var databasePath = Path.Combine(AppContext.BaseDirectory, "TestData", "GeoIP2-Country-Test.mmdb");

        return new MaxMindClientLocationResolver(databasePath);
    }
}