using SmartLinks.Management.Domain.SmartLinks;

namespace SmartLinks.Management.UnitTests.Domain.SmartLinks;

public class SmartLinkTests
{
    /// <summary>
    /// Проверяет создание умной ссылки с допустимыми базовыми значениями
    /// </summary>
    [Fact]
    public void CreateWithValidValuesCreatesSmartLink()
    {
        var id = Guid.NewGuid();
        const string slug = "summer-sale";
        const string defaultUrl = "https://example.com/default";

        var smartLink = SmartLink.Create(id, slug, defaultUrl, true);

        Assert.Equal(id, smartLink.Id);
        Assert.Equal(slug, smartLink.Slug);
        Assert.Equal(defaultUrl, smartLink.DefaultUrl);
        Assert.True(smartLink.IsActive);
        Assert.Empty(smartLink.Rules);
    }

    /// <summary>
    /// Проверяет запрет создания умной ссылки с пустым идентификатором
    /// </summary>
    [Fact]
    public void CreateWithEmptyIdThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            SmartLink.Create(Guid.Empty, "summer-sale", "https://example.com/default", true));

        Assert.Equal("id", exception.ParamName);
        Assert.StartsWith("Идентификатор умной ссылки не может быть пустым", exception.Message);
    }

    /// <summary>
    /// Проверяет запрет создания умной ссылки с недопустимым коротким адресом
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void CreateWithInvalidSlugThrowsArgumentException(string? slug)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            SmartLink.Create(Guid.NewGuid(), slug!, "https://example.com/default", true));

        Assert.Equal("slug", exception.ParamName);
        Assert.StartsWith("Короткий адрес умной ссылки не может быть пустым", exception.Message);
    }

    /// <summary>
    /// Проверяет запрет создания умной ссылки с недопустимым URL по умолчанию
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("/offers/default")]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com/resource")]
    [InlineData("https://")]
    [InlineData("http:offers")]
    public void CreateWithInvalidDefaultUrlThrowsArgumentException(string? defaultUrl)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            SmartLink.Create(Guid.NewGuid(), "summer-sale", defaultUrl!, true));

        Assert.Equal("defaultUrl", exception.ParamName);
        Assert.StartsWith(
            "URL по умолчанию должен быть абсолютным HTTP- или HTTPS-адресом",
            exception.Message);
    }

    /// <summary>
    /// Проверяет поддержку HTTP-схемы для URL по умолчанию
    /// </summary>
    [Fact]
    public void CreateWithHttpDefaultUrlCreatesSmartLink()
    {
        const string defaultUrl = "http://example.com/default";

        var smartLink = SmartLink.Create(Guid.NewGuid(), "summer-sale", defaultUrl, true);

        Assert.Equal(defaultUrl, smartLink.DefaultUrl);
    }

    /// <summary>
    /// Проверяет создание выключенной умной ссылки
    /// </summary>
    [Fact]
    public void CreateWithInactiveStateCreatesInactiveSmartLink()
    {
        var smartLink = SmartLink.Create(
            Guid.NewGuid(),
            "summer-sale",
            "https://example.com/default",
            false);

        Assert.False(smartLink.IsActive);
    }

    /// <summary>
    /// Проверяет добавление правила с переданными значениями
    /// </summary>
    [Fact]
    public void AddRuleStoresSpecifiedRule()
    {
        var smartLink = CreateSmartLink();
        const string targetUrl = "https://example.com/summer";
        const string conditionDsl = """{"dslVersion":1}""";

        smartLink.AddRule(20, true, targetUrl, conditionDsl);

        var rule = Assert.Single(smartLink.Rules);

        Assert.Equal(20, rule.Priority);
        Assert.True(rule.IsEnabled);
        Assert.Equal(targetUrl, rule.TargetUrl);
        Assert.Equal(conditionDsl, rule.ConditionDsl);
    }

    /// <summary>
    /// Проверяет упорядочивание правил по возрастанию приоритета
    /// </summary>
    [Fact]
    public void AddRulesOrdersThemByPriority()
    {
        var smartLink = CreateSmartLink();
        const string conditionDsl = """{"dslVersion":1}""";

        smartLink.AddRule(30, true, "https://example.com/30", conditionDsl);
        smartLink.AddRule(10, true, "https://example.com/10", conditionDsl);
        smartLink.AddRule(20, true, "https://example.com/20", conditionDsl);

        int[] expectedPriorities = [10, 20, 30];

        Assert.Equal(expectedPriorities, smartLink.Rules.Select(rule => rule.Priority));
    }

    /// <summary>
    /// Проверяет запрет одинаковых приоритетов правил
    /// </summary>
    [Fact]
    public void AddRuleWithDuplicatePriorityThrowsArgumentException()
    {
        var smartLink = CreateSmartLink();
        const string conditionDsl = """{"dslVersion":1}""";

        smartLink.AddRule(10, true, "https://example.com/first", conditionDsl);

        var exception = Assert.Throws<ArgumentException>(() =>
            smartLink.AddRule(10, true, "https://example.com/second", conditionDsl));

        Assert.Equal("priority", exception.ParamName);
        Assert.StartsWith("Правило с приоритетом 10 уже существует", exception.Message);
        Assert.Single(smartLink.Rules);
    }

    /// <summary>
    /// Проверяет запрет правила с недопустимым целевым URL
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("/offers/target")]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com/resource")]
    [InlineData("https://")]
    [InlineData("http:offers")]
    public void AddRuleWithInvalidTargetUrlThrowsArgumentException(string? targetUrl)
    {
        var smartLink = CreateSmartLink();

        var exception = Assert.Throws<ArgumentException>(() =>
            smartLink.AddRule(10, true, targetUrl!, """{"dslVersion":1}"""));

        Assert.Equal("targetUrl", exception.ParamName);
        Assert.StartsWith("Целевой URL правила должен быть абсолютным HTTP- или HTTPS-адресом", exception.Message);
        Assert.Empty(smartLink.Rules);
    }

    /// <summary>
    /// Проверяет поддержку HTTP-схемы для целевого URL правила
    /// </summary>
    [Fact]
    public void AddRuleWithHttpTargetUrlAddsRule()
    {
        var smartLink = CreateSmartLink();
        const string targetUrl = "http://example.com/target";

        smartLink.AddRule(10, true, targetUrl, """{"dslVersion":1}""");

        Assert.Equal(targetUrl, Assert.Single(smartLink.Rules).TargetUrl);
    }

    /// <summary>
    /// Проверяет запрет правила с пустым DSL условия
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void AddRuleWithInvalidConditionDslThrowsArgumentException(string? conditionDsl)
    {
        var smartLink = CreateSmartLink();

        var exception = Assert.Throws<ArgumentException>(() =>
            smartLink.AddRule(10, true, "https://example.com/target", conditionDsl!));

        Assert.Equal("conditionDsl", exception.ParamName);
        Assert.StartsWith("DSL условия правила не может быть пустым", exception.Message);
        Assert.Empty(smartLink.Rules);
    }

    /// <summary>
    /// Проверяет невозможность изменения правил в обход агрегата
    /// </summary>
    [Fact]
    public void RulesCannotBeModifiedOutsideAggregate()
    {
        var smartLink = CreateSmartLink();

        smartLink.AddRule(10, true, "https://example.com/target", """{"dslVersion":1}""");

        var rules = Assert.IsAssignableFrom<IList<SmartLinkRule>>(smartLink.Rules);
        var rule = Assert.Single(smartLink.Rules);

        Assert.True(rules.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => rules.Add(rule));
        Assert.Single(smartLink.Rules);
    }

    /// <summary>
    /// Создаёт умную ссылку с допустимыми базовыми значениями
    /// </summary>
    private static SmartLink CreateSmartLink() =>
        SmartLink.Create(Guid.NewGuid(), "summer-sale", "https://example.com/default", true);
}