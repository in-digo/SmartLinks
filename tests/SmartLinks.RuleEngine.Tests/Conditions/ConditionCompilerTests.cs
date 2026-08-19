using System.Text.Json;
using SmartLinks.RuleEngine.Conditions;
using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.RuleEngine.Tests.Conditions;

public sealed class ConditionCompilerTests
{
    // Выбирает фабрику, соответствующую типу условия
    [Fact]
    public void CompileUsesFactoryMatchingConditionType()
    {
        using var document = JsonDocument.Parse("{}");

        var parameters = document.RootElement.Clone();
        var expectedCondition = new StubCondition();
        var unexpectedCondition = new StubCondition();
        var countryFactory = new StubConditionFactory("country", unexpectedCondition);
        var timeFactory = new StubConditionFactory("utc-date-time-range", expectedCondition);
        var compiler = new ConditionCompiler(new[] { countryFactory, timeFactory });

        var result = compiler.Compile("utc-date-time-range", parameters);

        Assert.Same(expectedCondition, result);
    }

    private sealed class StubConditionFactory : IConditionFactory
    {
        private readonly ICompiledCondition condition;

        public string Type { get; }

        // Создаёт фабрику с заданным типом и результатом
        public StubConditionFactory(string type, ICompiledCondition condition)
        {
            Type = type;
            this.condition = condition;
        }

        // Возвращает заданное скомпилированное условие
        public ICompiledCondition Create(JsonElement parameters)
        {
            return condition;
        }
    }

    private sealed class StubCondition : ICompiledCondition
    {
        // Возвращает результат, не влияющий на проверяемое поведение
        public bool IsMatch(UrlResolutionContext context)
        {
            return false;
        }
    }

        // Выдаёт корректную ошибку, если фабрика типа условия не зарегистрирована
        [Fact]
        public void CompileThrowsWhenConditionTypeIsNotRegistered()
        {
            using var document = JsonDocument.Parse("{}");

            var parameters = document.RootElement.Clone();
            var compiler = new ConditionCompiler(Array.Empty<IConditionFactory>());

            var exception = Assert.Throws<InvalidOperationException>(
                () => compiler.Compile("unknown", parameters));

            Assert.Equal("Фабрика условия типа 'unknown' не зарегистрирована", exception.Message);
        }
}