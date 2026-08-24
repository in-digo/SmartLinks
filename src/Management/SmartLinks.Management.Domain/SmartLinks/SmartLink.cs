namespace SmartLinks.Management.Domain.SmartLinks;

public sealed class SmartLink
{
    private readonly List<SmartLinkRule> _rules = [];
    private readonly IReadOnlyList<SmartLinkRule> _readOnlyRules;

    /// <summary>
    /// Инициализирует умную ссылку
    /// </summary>
    private SmartLink(Guid id, string slug, string defaultUrl, bool isActive)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Идентификатор умной ссылки не может быть пустым", nameof(id));

        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Короткий адрес умной ссылки не может быть пустым", nameof(slug));

        if (!HttpUrlValidator.IsValid(defaultUrl))
            throw new ArgumentException("URL по умолчанию должен быть абсолютным HTTP- или HTTPS-адресом", nameof(defaultUrl));

        Id = id;
        Slug = slug;
        DefaultUrl = defaultUrl;
        IsActive = isActive;
        _readOnlyRules = _rules.AsReadOnly();
    }

    public Guid Id { get; }

    public string Slug { get; }

    public string DefaultUrl { get; }

    public bool IsActive { get; }

    public IReadOnlyList<SmartLinkRule> Rules => _readOnlyRules;

    /// <summary>
    /// Создаёт умную ссылку
    /// </summary>
    public static SmartLink Create(Guid id, string slug, string defaultUrl, bool isActive) =>
        new(id, slug, defaultUrl, isActive);

    /// <summary>
    /// Добавляет правило умной ссылки
    /// </summary>
    public void AddRule(int priority, bool isEnabled, string targetUrl, string conditionDsl)
    {
        if (_rules.Exists(rule => rule.Priority == priority))
            throw new ArgumentException($"Правило с приоритетом {priority} уже существует", nameof(priority));

        _rules.Add(new SmartLinkRule(priority, isEnabled, targetUrl, conditionDsl));
        _rules.Sort(static (left, right) => left.Priority.CompareTo(right.Priority));
    }
}