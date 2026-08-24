namespace SmartLinks.Management.Application.Exceptions;

public sealed class SmartLinkNotFoundException : InvalidOperationException
{
    /// <summary>
    /// Инициализирует исключение со стандартным сообщением
    /// </summary>
    public SmartLinkNotFoundException() : base("Умная ссылка не найдена")
    {
    }

    /// <summary>
    /// Инициализирует исключение с указанным сообщением
    /// </summary>
    public SmartLinkNotFoundException(string? message) : base(message)
    {
    }

    /// <summary>
    /// Инициализирует исключение с указанным сообщением и внутренним исключением
    /// </summary>
    public SmartLinkNotFoundException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Инициализирует исключение для указанного идентификатора
    /// </summary>
    private SmartLinkNotFoundException(string message, Guid smartLinkId) : base(message)
    {
        SmartLinkId = smartLinkId;
    }

    public Guid SmartLinkId { get; }

    /// <summary>
    /// Создаёт исключение для отсутствующей умной ссылки
    /// </summary>
    public static SmartLinkNotFoundException ForId(Guid smartLinkId) =>
        new($"Умная ссылка с идентификатором '{smartLinkId}' не найдена", smartLinkId);
}