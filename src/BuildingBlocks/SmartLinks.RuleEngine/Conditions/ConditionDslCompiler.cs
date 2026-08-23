using System.Text.Json;
using SmartLinks.RuleEngine.Resolution;

namespace SmartLinks.RuleEngine.Conditions;

/// <summary>
/// Компилирует JSON DSL в готовое условие
/// </summary>
public sealed class ConditionDslCompiler
{
    private const int _supportedDslVersion = 1;

    private readonly ConditionCompiler _conditionCompiler;

    /// <summary>
    /// Создаёт компилятор DSL с компилятором predicate-условий
    /// </summary>
    public ConditionDslCompiler(ConditionCompiler conditionCompiler)
    {
        _conditionCompiler = conditionCompiler;
    }

    /// <summary>
    /// Компилирует JSON DSL в готовое условие
    /// </summary>
    public ICompiledCondition Compile(string json)
    {
        using var document = JsonDocument.Parse(json);

        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("Корень DSL должен быть JSON-объектом");

        ValidateVersion(root);

        if (!root.TryGetProperty("condition", out var condition))
            throw new InvalidOperationException("Поле 'condition' обязательно");

        return CompileNode(condition);
    }

    /// <summary>
    /// Проверяет наличие, тип и поддерживаемое значение версии DSL
    /// </summary>
    private static void ValidateVersion(JsonElement root)
    {
        if (!root.TryGetProperty("dslVersion", out var dslVersionElement))
            throw new InvalidOperationException("Поле 'dslVersion' обязательно");

        if (dslVersionElement.ValueKind != JsonValueKind.Number ||
            !dslVersionElement.TryGetInt32(out var dslVersion))
        {
            throw new InvalidOperationException("Поле 'dslVersion' должно содержать целое число");
        }

        if (dslVersion != _supportedDslVersion)
            throw new InvalidOperationException($"Версия DSL '{dslVersion}' не поддерживается");
    }

    /// <summary>
    /// Рекурсивно компилирует один узел условия
    /// </summary>
    private ICompiledCondition CompileNode(JsonElement node)
    {
        if (node.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("Узел условия должен быть JSON-объектом");

        var hasAll = node.TryGetProperty("all", out var allElement);
        var hasAny = node.TryGetProperty("any", out var anyElement);
        var hasNot = node.TryGetProperty("not", out var notElement);
        var hasType = node.TryGetProperty("type", out _);
        var operationsCount = 0;

        if (hasAll)
            operationsCount++;

        if (hasAny)
            operationsCount++;

        if (hasNot)
            operationsCount++;

        if (hasType)
            operationsCount++;

        if (operationsCount != 1)
        {
            throw new InvalidOperationException(
                "Узел условия должен содержать ровно одну операцию: 'all', 'any', 'not' или 'type'");
        }

        if (hasAll)
            return CompileComposite(allElement, "all", conditions => new AllCondition(conditions));

        if (hasAny)
            return CompileComposite(anyElement, "any", conditions => new AnyCondition(conditions));

        if (hasNot)
        {
            if (notElement.ValueKind != JsonValueKind.Object)
                throw new InvalidOperationException("Поле 'not' должно содержать один узел условия");
            return new NotCondition(CompileNode(notElement));
        }

        return CompilePredicate(node);
    }

    /// <summary>
    /// Компилирует непустой массив вложенных условий
    /// </summary>
    private ICompiledCondition CompileComposite(
        JsonElement element,
        string operation,
        Func<IEnumerable<ICompiledCondition>, ICompiledCondition> createCondition)
    {
        if (element.ValueKind != JsonValueKind.Array || element.GetArrayLength() == 0)
            throw new InvalidOperationException($"Поле '{operation}' должно содержать непустой массив условий");

        return createCondition(element.EnumerateArray().Select(CompileNode));
    }

    /// <summary>
    /// Компилирует predicate-узел с помощью зарегистрированной фабрики
    /// </summary>
    private ICompiledCondition CompilePredicate(JsonElement node)
    {
        var typeElement = node.GetProperty("type");
        var type = typeElement.ValueKind == JsonValueKind.String ? typeElement.GetString() : null;
        if (string.IsNullOrWhiteSpace(type))
            throw new InvalidOperationException("Поле 'type' должно содержать непустую строку");

        if (!node.TryGetProperty("parameters", out var parameters))
            throw new InvalidOperationException("Поле 'parameters' обязательно для predicate-узла");

        if (parameters.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("Поле 'parameters' должно содержать JSON-объект");

        return _conditionCompiler.Compile(type, parameters);
    }
}