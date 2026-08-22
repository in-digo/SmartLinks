using SmartLinks.RuleEngine.Conditions;
using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.RuleEngine.Tests.Conditions;

public sealed class UtcDateTimeRangeConditionTests
{
    // Возвращает true, если время запроса находится внутри интервала
    [Fact]
    public void IsMatchReturnsTrueWhenRequestTimeIsInsideRange()
    {
        var fromUtc = new DateTimeOffset(2026, 8, 19, 10, 0, 0, TimeSpan.Zero);
        var toUtc = new DateTimeOffset(2026, 8, 19, 12, 0, 0, TimeSpan.Zero);
        var requestTimeUtc = new DateTimeOffset(2026, 8, 19, 11, 0, 0, TimeSpan.Zero);

        var utcTimeFeature = new UtcTimeFeature(requestTimeUtc);
        var condition = new UtcDateTimeRangeCondition(fromUtc, toUtc);
        var context = new UrlResolutionContext(new IResolutionFeature[] { utcTimeFeature });

        var result = condition.IsMatch(context);

        Assert.True(result);
    }

    // Проверяет включение начальной и исключение конечной границы интервала
    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(119, true)]
    [InlineData(120, false)]
    public void IsMatchReturnsExpectedResultAtRangeBoundaries(int minutesFromStart, bool expected)
    {
        var fromUtc = new DateTimeOffset(2026, 8, 19, 10, 0, 0, TimeSpan.Zero);
        var toUtc = new DateTimeOffset(2026, 8, 19, 12, 0, 0, TimeSpan.Zero);
        var requestTimeUtc = fromUtc.AddMinutes(minutesFromStart);

        var utcTimeFeature = new UtcTimeFeature(requestTimeUtc);
        var condition = new UtcDateTimeRangeCondition(fromUtc, toUtc);
        var context = new UrlResolutionContext(new IResolutionFeature[] { utcTimeFeature });
        
        var result = condition.IsMatch(context);

        Assert.Equal(expected, result);
    }

    // Запрещает создание пустого или обратного временного интервала
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ConstructorThrowsWhenEndIsNotAfterStart(int durationMinutes)
    {
        var fromUtc = new DateTimeOffset(2026, 8, 19, 10, 0, 0, TimeSpan.Zero);
        var toUtc = fromUtc.AddMinutes(durationMinutes);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new UtcDateTimeRangeCondition(fromUtc, toUtc));

        Assert.Equal("toUtc", exception.ParamName);
    }
}