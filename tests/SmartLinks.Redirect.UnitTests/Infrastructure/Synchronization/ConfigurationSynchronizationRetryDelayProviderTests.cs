using Microsoft.Extensions.Options;
using SmartLinks.Redirect.Infrastructure.Synchronization;

namespace SmartLinks.Redirect.UnitTests.Infrastructure.Synchronization;

public sealed class ConfigurationSynchronizationRetryDelayProviderTests
{
    /// <summary>
    /// Проверяет full jitter внутри начальной exponential backoff границы
    /// </summary>
    [Fact]
    public void GetDelayReturnsJitteredDelayWithinInitialRetryWindow()
    {
        var options = Options.Create(new ConfigurationSynchronizationOptions
        {
            InitialRetryDelay = TimeSpan.FromSeconds(2),
            MaximumRetryDelay = TimeSpan.FromSeconds(30)
        });
        var provider = new ConfigurationSynchronizationRetryDelayProvider(options, new FixedRandom(0.5));

        var delay = provider.GetDelay(retryAttempt: 1);

        Assert.Equal(TimeSpan.FromSeconds(1), delay);
    }

    /// <summary>
    /// Проверяет ошибку создания provider без настроек синхронизации
    /// </summary>
    [Fact]
    public void ConstructorWithNullOptionsThrowsArgumentNullException()
    {
        IOptions<ConfigurationSynchronizationOptions> options = null!;
        var random = new FixedRandom(0.5);

        var exception = Assert.Throws<ArgumentNullException>(
            () => new ConfigurationSynchronizationRetryDelayProvider(options, random));

        Assert.Equal("options", exception.ParamName);
    }

    /// <summary>
    /// Проверяет ошибку создания provider без генератора jitter
    /// </summary>
    [Fact]
    public void ConstructorWithNullRandomThrowsArgumentNullException()
    {
        var options = Options.Create(new ConfigurationSynchronizationOptions());
        Random random = null!;

        var exception = Assert.Throws<ArgumentNullException>(
            () => new ConfigurationSynchronizationRetryDelayProvider(options, random));

        Assert.Equal("random", exception.ParamName);
    }

    /// <summary>
    /// Проверяет экспоненциальное увеличение окна повторной попытки
    /// </summary>
    [Theory]
    [InlineData(2, 2)]
    [InlineData(3, 4)]
    [InlineData(4, 8)]
    public void GetDelayUsesExponentialRetryWindow(int retryAttempt, int expectedDelaySeconds)
    {
        var options = Options.Create(new ConfigurationSynchronizationOptions
        {
            InitialRetryDelay = TimeSpan.FromSeconds(2),
            MaximumRetryDelay = TimeSpan.FromSeconds(30)
        });
        var provider = new ConfigurationSynchronizationRetryDelayProvider(options, new FixedRandom(0.5));

        var delay = provider.GetDelay(retryAttempt);

        Assert.Equal(TimeSpan.FromSeconds(expectedDelaySeconds), delay);
    }

    /// <summary>
    /// Проверяет ограничение окна повторной попытки максимальной задержкой
    /// </summary>
    [Theory]
    [InlineData(5)]
    [InlineData(100)]
    public void GetDelayCapsRetryWindowAtMaximumDelay(int retryAttempt)
    {
        var options = Options.Create(new ConfigurationSynchronizationOptions
        {
            InitialRetryDelay = TimeSpan.FromSeconds(2),
            MaximumRetryDelay = TimeSpan.FromSeconds(30)
        });
        var provider = new ConfigurationSynchronizationRetryDelayProvider(options, new FixedRandom(0.5));

        var delay = provider.GetDelay(retryAttempt);

        Assert.Equal(TimeSpan.FromSeconds(15), delay);
    }

    /// <summary>
    /// Проверяет отклонение неположительного номера повторной попытки
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GetDelayWithNonPositiveRetryAttemptThrowsArgumentOutOfRangeException(int retryAttempt)
    {
        var options = Options.Create(new ConfigurationSynchronizationOptions
        {
            InitialRetryDelay = TimeSpan.FromSeconds(2),
            MaximumRetryDelay = TimeSpan.FromSeconds(30)
        });
        var provider = new ConfigurationSynchronizationRetryDelayProvider(options, new FixedRandom(0.5));

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => provider.GetDelay(retryAttempt));

        Assert.Equal("retryAttempt", exception.ParamName);
    }

    private sealed class FixedRandom : Random
    {
        private readonly double _value;

        /// <summary>
        /// Инициализирует генератор фиксированным значением
        /// </summary>
        public FixedRandom(double value)
        {
            _value = value;
        }

        /// <summary>
        /// Возвращает фиксированное значение jitter
        /// </summary>
        protected override double Sample()
        {
            return _value;
        }
    }
}