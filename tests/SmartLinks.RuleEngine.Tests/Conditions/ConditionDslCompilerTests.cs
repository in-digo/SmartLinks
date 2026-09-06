using System.Text.Json;
using SmartLinks.RuleEngine.Conditions;
using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.RuleEngine.Tests.Conditions;

public sealed class ConditionDslCompilerTests
{
    /// <summary>
    /// Проверяет передачу predicate-узла версии 1 зарегистрированной фабрике
    /// </summary>
    [Fact]
    public void CompileVersionOnePredicateUsesRegisteredFactory()
    {
        const string json = """
            {
              "dslVersion": 1,
              "condition": {
                "type": "test",
                "parameters": {
                  "value": "expected"
                }
              }
            }
            """;

        var expectedCondition = new StubCondition(false);
        var factory = new RecordingConditionFactory("test", expectedCondition);
        var compiler = CreateCompiler(factory);

        var result = compiler.Compile(json);

        Assert.Same(expectedCondition, result);
        Assert.Equal("expected", factory.ParameterValue);
    }

    /// <summary>
    /// Проверяет ошибку при неподдерживаемой версии DSL
    /// </summary>
    [Fact]
    public void CompileThrowsWhenDslVersionIsNotSupported()
    {
        const string json = """
            {
              "dslVersion": 2,
              "condition": {
                "type": "test",
                "parameters": {
                  "value": "expected"
                }
              }
            }
            """;

        var factory = new RecordingConditionFactory("test", new StubCondition(false));
        var compiler = CreateCompiler(factory);

        var exception = Assert.Throws<InvalidOperationException>(() => compiler.Compile(json));

        Assert.Equal("Версия DSL '2' не поддерживается", exception.Message);
    }

    /// <summary>
    /// Проверяет ошибку при отсутствии версии DSL
    /// </summary>
    [Fact]
    public void CompileThrowsWhenDslVersionIsMissing()
    {
        const string json = """
            {
              "condition": {
                "type": "test",
                "parameters": {}
              }
            }
            """;

        var compiler = CreateCompiler();

        var exception = Assert.Throws<InvalidOperationException>(() => compiler.Compile(json));

        Assert.Equal("Поле 'dslVersion' обязательно", exception.Message);
    }

    /// <summary>
    /// Проверяет ошибку, если версия DSL указана не числом
    /// </summary>
    [Fact]
    public void CompileThrowsWhenDslVersionIsNotNumber()
    {
        const string json = """
            {
              "dslVersion": "1",
              "condition": {
                "type": "test",
                "parameters": {}
              }
            }
            """;

        var compiler = CreateCompiler();

        var exception = Assert.Throws<InvalidOperationException>(() => compiler.Compile(json));

        Assert.Equal("Поле 'dslVersion' должно содержать целое число", exception.Message);
    }

    /// <summary>
    /// Проверяет ошибку, если версия DSL указана не целым числом
    /// </summary>
    [Fact]
    public void CompileThrowsWhenDslVersionIsNotInteger()
    {
        const string json = """
            {
              "dslVersion": 1.5,
              "condition": {
                "type": "test",
                "parameters": {}
              }
            }
            """;

        var compiler = CreateCompiler();

        var exception = Assert.Throws<InvalidOperationException>(() => compiler.Compile(json));

        Assert.Equal("Поле 'dslVersion' должно содержать целое число", exception.Message);
    }

    /// <summary>
    /// Проверяет ошибку синтаксического разбора некорректного JSON
    /// </summary>
    [Fact]
    public void CompileThrowsWhenJsonIsMalformed()
    {
        const string json = "{\"dslVersion\": 1";
        var compiler = CreateCompiler();

        Assert.ThrowsAny<JsonException>(() => compiler.Compile(json));
    }

    /// <summary>
    /// Проверяет ошибку, если корень DSL не является объектом
    /// </summary>
    [Fact]
    public void CompileThrowsWhenRootIsNotObject()
    {
        const string json = """
            [
              {
                "dslVersion": 1
              }
            ]
            """;

        var compiler = CreateCompiler();

        var exception = Assert.Throws<InvalidOperationException>(() => compiler.Compile(json));

        Assert.Equal("Корень DSL должен быть JSON-объектом", exception.Message);
    }

    /// <summary>
    /// Проверяет ошибку при отсутствии корневого условия
    /// </summary>
    [Fact]
    public void CompileThrowsWhenConditionIsMissing()
    {
        const string json = """
            {
              "dslVersion": 1
            }
            """;

        var compiler = CreateCompiler();

        var exception = Assert.Throws<InvalidOperationException>(() => compiler.Compile(json));

        Assert.Equal("Поле 'condition' обязательно", exception.Message);
    }

    /// <summary>
    /// Проверяет ошибку, если узел условия не является объектом
    /// </summary>
    [Fact]
    public void CompileThrowsWhenConditionIsNotObject()
    {
        const string json = """
            {
              "dslVersion": 1,
              "condition": []
            }
            """;

        var compiler = CreateCompiler();

        var exception = Assert.Throws<InvalidOperationException>(() => compiler.Compile(json));

        Assert.Equal("Узел условия должен быть JSON-объектом", exception.Message);
    }

    /// <summary>
    /// Проверяет ошибку при неизвестной операции узла
    /// </summary>
    [Fact]
    public void CompileThrowsWhenNodeOperationIsUnknown()
    {
        const string json = """
            {
              "dslVersion": 1,
              "condition": {
                "xor": []
              }
            }
            """;

        var compiler = CreateCompiler();

        var exception = Assert.Throws<InvalidOperationException>(() => compiler.Compile(json));

        Assert.Equal(
            "Узел условия должен содержать ровно одну операцию: 'all', 'any', 'not' или 'type'",
            exception.Message);
    }

    /// <summary>
    /// Проверяет ошибку при нескольких операциях в одном узле
    /// </summary>
    [Fact]
    public void CompileThrowsWhenNodeContainsMultipleOperations()
    {
        const string json = """
            {
              "dslVersion": 1,
              "condition": {
                "all": [
                  {
                    "type": "test",
                    "parameters": {
                      "value": "expected"
                    }
                  }
                ],
                "type": "test",
                "parameters": {
                  "value": "expected"
                }
              }
            }
            """;

        var compiler = CreateCompiler();

        var exception = Assert.Throws<InvalidOperationException>(() => compiler.Compile(json));

        Assert.Equal(
            "Узел условия должен содержать ровно одну операцию: 'all', 'any', 'not' или 'type'",
            exception.Message);
    }

    /// <summary>
    /// Проверяет ошибку при некорректном типе predicate-узла
    /// </summary>
    [Theory]
    [InlineData("\"\"")]
    [InlineData("123")]
    public void CompileThrowsWhenPredicateTypeIsInvalid(string typeValue)
    {
        var json = $$"""
            {
              "dslVersion": 1,
              "condition": {
                "type": {{typeValue}},
                "parameters": {}
              }
            }
            """;

        var compiler = CreateCompiler();

        var exception = Assert.Throws<InvalidOperationException>(() => compiler.Compile(json));

        Assert.Equal("Поле 'type' должно содержать непустую строку", exception.Message);
    }

    /// <summary>
    /// Проверяет ошибку при отсутствии параметров predicate-узла
    /// </summary>
    [Fact]
    public void CompileThrowsWhenPredicateParametersAreMissing()
    {
        const string json = """
            {
              "dslVersion": 1,
              "condition": {
                "type": "test"
              }
            }
            """;

        var compiler = CreateCompiler();

        var exception = Assert.Throws<InvalidOperationException>(() => compiler.Compile(json));

        Assert.Equal("Поле 'parameters' обязательно для predicate-узла", exception.Message);
    }

    /// <summary>
    /// Проверяет ошибку, если параметры predicate-узла не являются объектом
    /// </summary>
    [Fact]
    public void CompileThrowsWhenPredicateParametersAreNotObject()
    {
        const string json = """
            {
              "dslVersion": 1,
              "condition": {
                "type": "test",
                "parameters": []
              }
            }
            """;

        var factory = new RecordingConditionFactory("test", new StubCondition(false));
        var compiler = CreateCompiler(factory);

        var exception = Assert.Throws<InvalidOperationException>(() => compiler.Compile(json));

        Assert.Equal("Поле 'parameters' должно содержать JSON-объект", exception.Message);
    }

    /// <summary>
    /// Проверяет сохранение ошибки неизвестного predicate-типа
    /// </summary>
    [Fact]
    public void CompilePropagatesUnknownPredicateError()
    {
        const string json = """
            {
              "dslVersion": 1,
              "condition": {
                "type": "unknown",
                "parameters": {}
              }
            }
            """;

        var compiler = CreateCompiler();

        var exception = Assert.Throws<InvalidOperationException>(() => compiler.Compile(json));

        Assert.Equal("Фабрика условия типа 'unknown' не зарегистрирована", exception.Message);
    }

    /// <summary>
    /// Проверяет компиляцию операции all
    /// </summary>
    [Fact]
    public void CompileCreatesAllCondition()
    {
        const string json = """
            {
              "dslVersion": 1,
              "condition": {
                "all": [
                  {
                    "type": "first",
                    "parameters": {
                      "value": "first"
                    }
                  },
                  {
                    "type": "second",
                    "parameters": {
                      "value": "second"
                    }
                  }
                ]
              }
            }
            """;

        var firstFactory = new RecordingConditionFactory("first", new StubCondition(true));
        var secondFactory = new RecordingConditionFactory("second", new StubCondition(false));
        var compiler = CreateCompiler(firstFactory, secondFactory);

        var result = compiler.Compile(json);

        Assert.IsType<AllCondition>(result);
        Assert.False(result.IsMatch(CreateContext()));
    }

    /// <summary>
    /// Проверяет компиляцию операции any
    /// </summary>
    [Fact]
    public void CompileCreatesAnyCondition()
    {
        const string json = """
            {
              "dslVersion": 1,
              "condition": {
                "any": [
                  {
                    "type": "first",
                    "parameters": {
                      "value": "first"
                    }
                  },
                  {
                    "type": "second",
                    "parameters": {
                      "value": "second"
                    }
                  }
                ]
              }
            }
            """;

        var firstFactory = new RecordingConditionFactory("first", new StubCondition(false));
        var secondFactory = new RecordingConditionFactory("second", new StubCondition(true));
        var compiler = CreateCompiler(firstFactory, secondFactory);

        var result = compiler.Compile(json);

        Assert.IsType<AnyCondition>(result);
        Assert.True(result.IsMatch(CreateContext()));
    }

    /// <summary>
    /// Проверяет компиляцию операции not
    /// </summary>
    [Fact]
    public void CompileCreatesNotCondition()
    {
        const string json = """
            {
              "dslVersion": 1,
              "condition": {
                "not": {
                  "type": "test",
                  "parameters": {
                    "value": "test"
                  }
                }
              }
            }
            """;

        var factory = new RecordingConditionFactory("test", new StubCondition(false));
        var compiler = CreateCompiler(factory);

        var result = compiler.Compile(json);

        Assert.IsType<NotCondition>(result);
        Assert.True(result.IsMatch(CreateContext()));
    }

    /// <summary>
    /// Проверяет ошибку при некорректном массиве составного условия
    /// </summary>
    [Theory]
    [InlineData("all", "{}")]
    [InlineData("all", "[]")]
    [InlineData("any", "{}")]
    [InlineData("any", "[]")]
    public void CompileThrowsWhenCompositeArrayIsInvalid(string operation, string value)
    {
        var json = $$"""
            {
              "dslVersion": 1,
              "condition": {
                "{{operation}}": {{value}}
              }
            }
            """;

        var compiler = CreateCompiler();

        var exception = Assert.Throws<InvalidOperationException>(() => compiler.Compile(json));

        Assert.Equal(
            $"Поле '{operation}' должно содержать непустой массив условий",
            exception.Message);
    }

    /// <summary>
    /// Проверяет ошибку, если not не содержит один узел условия
    /// </summary>
    [Fact]
    public void CompileThrowsWhenNotValueIsNotObject()
    {
        const string json = """
            {
              "dslVersion": 1,
              "condition": {
                "not": []
              }
            }
            """;

        var compiler = CreateCompiler();

        var exception = Assert.Throws<InvalidOperationException>(() => compiler.Compile(json));

        Assert.Equal("Поле 'not' должно содержать один узел условия", exception.Message);
    }

    /// <summary>
    /// Проверяет рекурсивную валидацию элементов составного условия
    /// </summary>
    [Fact]
    public void CompileThrowsWhenCompositeChildIsNotObject()
    {
        const string json = """
            {
              "dslVersion": 1,
              "condition": {
                "all": [
                  true
                ]
              }
            }
            """;

        var compiler = CreateCompiler();

        var exception = Assert.Throws<InvalidOperationException>(() => compiler.Compile(json));

        Assert.Equal("Узел условия должен быть JSON-объектом", exception.Message);
    }

    /// <summary>
    /// Проверяет компиляцию вложенного дерева all, any и not
    /// </summary>
    [Fact]
    public void CompileBuildsNestedCompositeTree()
    {
        const string json = """
            {
              "dslVersion": 1,
              "condition": {
                "all": [
                  {
                    "type": "true",
                    "parameters": {
                      "value": "true"
                    }
                  },
                  {
                    "any": [
                      {
                        "type": "false",
                        "parameters": {
                          "value": "false"
                        }
                      },
                      {
                        "not": {
                          "type": "false",
                          "parameters": {
                            "value": "false"
                          }
                        }
                      }
                    ]
                  }
                ]
              }
            }
            """;

        var trueFactory = new RecordingConditionFactory("true", new StubCondition(true));
        var falseFactory = new RecordingConditionFactory("false", new StubCondition(false));
        var compiler = CreateCompiler(trueFactory, falseFactory);

        var result = compiler.Compile(json);

        Assert.IsType<AllCondition>(result);
        Assert.True(result.IsMatch(CreateContext()));
    }

    /// <summary>
    /// Проверяет изменение комбинации условий только через JSON
    /// </summary>
    [Fact]
    public void CompileChangesCombinationBehaviorUsingOnlyJson()
    {
        const string allJson = """
            {
              "dslVersion": 1,
              "condition": {
                "all": [
                  {
                    "type": "true",
                    "parameters": {
                      "value": "true"
                    }
                  },
                  {
                    "type": "false",
                    "parameters": {
                      "value": "false"
                    }
                  }
                ]
              }
            }
            """;

        const string anyJson = """
            {
              "dslVersion": 1,
              "condition": {
                "any": [
                  {
                    "type": "true",
                    "parameters": {
                      "value": "true"
                    }
                  },
                  {
                    "type": "false",
                    "parameters": {
                      "value": "false"
                    }
                  }
                ]
              }
            }
            """;

        var trueFactory = new RecordingConditionFactory("true", new StubCondition(true));
        var falseFactory = new RecordingConditionFactory("false", new StubCondition(false));
        var compiler = CreateCompiler(trueFactory, falseFactory);
        var context = CreateContext();

        var allCondition = compiler.Compile(allJson);
        var anyCondition = compiler.Compile(anyJson);

        Assert.False(allCondition.IsMatch(context));
        Assert.True(anyCondition.IsMatch(context));
    }

    /// <summary>
    /// Создаёт DSL-компилятор с указанными фабриками
    /// </summary>
    private static ConditionDslCompiler CreateCompiler(params IConditionFactory[] conditionFactories)
    {
        return new ConditionDslCompiler(new ConditionCompiler(conditionFactories));
    }

    /// <summary>
    /// Создаёт пустой контекст выполнения условий
    /// </summary>
    private static UrlResolutionContext CreateContext()
    {
        return new UrlResolutionContext(Array.Empty<IResolutionFeature>());
    }

    private sealed class RecordingConditionFactory : IConditionFactory
    {
        private readonly ICompiledCondition _condition;

        public string Type { get; }

        public string? ParameterValue { get; private set; }

        /// <summary>
        /// Создаёт фабрику с заданным типом и возвращаемым условием
        /// </summary>
        public RecordingConditionFactory(string type, ICompiledCondition condition)
        {
            Type = type;
            _condition = condition;
        }

        /// <summary>
        /// Сохраняет переданный параметр и возвращает заданное условие
        /// </summary>
        public ICompiledCondition Create(JsonElement parameters)
        {
            ParameterValue = parameters.GetProperty("value").GetString();
            return _condition;
        }
    }
}