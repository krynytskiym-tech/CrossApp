using Core;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Api;

// Додаткове завдання 3: DomainException -> 409. Клієнт ніколи не бачить стек, типи винятків і шляхи.
public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        (int Status, string Title, string? Detail) outcome = exception switch
        {
            DomainException domain => (StatusCodes.Status409Conflict, "Порушено бізнес-правило.", domain.Message),
            ArgumentException => (StatusCodes.Status400BadRequest, "Некоректні дані запиту.", null),
            BadHttpRequestException bad => (bad.StatusCode, "Некоректний запит.", null),
            _ => (StatusCodes.Status500InternalServerError, "Внутрішня помилка сервера.", null)
        };

        if (outcome.Status >= 500)
            logger.LogError(exception, "Необроблений виняток");   // деталі лише в журналі сервера

        var problem = new ProblemDetails { Status = outcome.Status, Title = outcome.Title, Detail = outcome.Detail };
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = outcome.Status;
        await httpContext.Response.WriteAsJsonAsync(
            problem, options: null, contentType: "application/problem+json", cancellationToken: cancellationToken);
        return true;
    }
}
