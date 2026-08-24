namespace SmartLinks.Management.Application.Exceptions;

public sealed class SmartLinkSlugAlreadyExistsException : InvalidOperationException
{
    /// <summary>
    /// Инициализирует исключение со стандартным сообщением
    /// </summary>
    public SmartLinkSlugAlreadyExistsException() : base("Умная ссылка с таким коротким адресом уже существует")
    {
        Slug = string.Empty;
    }

    /// <summary>
    /// Инициализирует исключение с указанным сообщением
    /// </summary>
    public SmartLinkSlugAlreadyExistsException(string? message) : base(message)
    {
        Slug = string.Empty;
    }

    /// <summary>
    /// Инициализирует исключение с указанным сообщением и внутренним исключением
    /// </summary>
    public SmartLinkSlugAlreadyExistsException(string? message, Exception? innerException) : base(message, innerException)
    {
        Slug = string.Empty;
    }

    /// <summary>
    /// Инициализирует исключение для указанного короткого адреса
    /// </summary>
    private SmartLinkSlugAlreadyExistsException(string message, string slug) : base(message)
    {
        Slug = slug;
    }

    public string Slug { get; }

    /// <summary>
    /// Создаёт исключение для занятого короткого адреса
    /// </summary>
    public static SmartLinkSlugAlreadyExistsException ForSlug(string slug) =>
        new($"Умная ссылка с коротким адресом '{slug}' уже существует", slug);
}