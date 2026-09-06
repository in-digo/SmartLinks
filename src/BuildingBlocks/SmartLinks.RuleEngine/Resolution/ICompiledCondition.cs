namespace SmartLinks.RuleEngine.Resolution;

// Представляет готовое к выполнению условие правила
public interface ICompiledCondition
{
    // Проверяет соответствие контекста условию
    bool IsMatch(UrlResolutionContext context);
}