namespace SmartLinks.Management.Api.Contracts.SmartLinks;

/// <summary>
/// Результат публикации умной ссылки
/// </summary>
/// <param name="Revision">Новая глобальная ревизия опубликованной конфигурации</param>
public sealed record PublishSmartLinkResponse(long Revision);