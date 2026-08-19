using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.RuleEngine.Conditions;

/// <summary>
/// Проверяет попадание времени запроса в заданный UTC-интервал
/// </summary>
public sealed class UtcDateTimeRangeCondition : ICompiledCondition
{
    private readonly DateTimeOffset _fromUtc;
    private readonly DateTimeOffset _toUtc;

    /// <summary>
    /// Создаёт условие для временного интервала [fromUtc, toUtc)
    /// </summary>
    public UtcDateTimeRangeCondition(DateTimeOffset fromUtc, DateTimeOffset toUtc)
    {
        _fromUtc = fromUtc.ToUniversalTime();
        _toUtc = toUtc.ToUniversalTime();

        if (_fromUtc >= _toUtc)
            throw new ArgumentOutOfRangeException(nameof(toUtc), "Время окончания интервала должно быть больше времени начала");
    }

    /// <summary>
    /// Проверяет попадание времени запроса в заданный интервал.
    /// </summary>
    public bool IsMatch(UrlResolutionContext context)
    {
        var utcNow = context.UtcNow.ToUniversalTime();

        return utcNow >= _fromUtc && utcNow < _toUtc;
    }
}