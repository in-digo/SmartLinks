using Microsoft.AspNetCore.Diagnostics;
using SmartLinks.Management.Application.Exceptions;

namespace SmartLinks.Management.Api.ExceptionHandling;

/// <summary>
/// Преобразует ожидаемые исключения Management в Problem Details
/// </summary>
public sealed class ManagementApiExceptionHandler : IExceptionHandler
{
    /// <summary>
    /// Формирует HTTP-ответ для ожидаемого исключения
    /// </summary>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var problemResult = CreateProblemResult(exception);

        if (problemResult is null)
            return false;

        await problemResult.ExecuteAsync(httpContext);
        return true;
    }

    /// <summary>
    /// Создаёт Problem Details для ожидаемого исключения
    /// </summary>
    private static IResult? CreateProblemResult(Exception exception)
    {
        return exception switch
        {
            SmartLinkNotFoundException => Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Умная ссылка не найдена",
                detail: exception.Message),
            SmartLinkSlugAlreadyExistsException => Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Умная ссылка уже существует",
                detail: exception.Message),
            ArgumentException => Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Некорректный запрос",
                detail: exception.Message),
            _ => null
        };
    }
}