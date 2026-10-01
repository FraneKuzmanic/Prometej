using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Prometej_core.Exceptions;

namespace Prometej_api.ErrorHandling
{
    public sealed class DomainExceptionHandler(ILogger<DomainExceptionHandler> logger) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            int? status = exception switch
            {
                BadRequestException => StatusCodes.Status400BadRequest,
                InvalidCredentialsException => StatusCodes.Status401Unauthorized,
                ForbiddenException => StatusCodes.Status403Forbidden,
                NotFoundException => StatusCodes.Status404NotFound,
                ConflictException => StatusCodes.Status409Conflict,
                _ => null,
            };

            if (status is null)
            {
                // Not one of ours: log it here and let the default handler answer with a 500
                // that carries no message. The framework's own log of every exception is
                // switched off in appsettings.json, because it also reported each expected
                // 4xx above as an error.
                logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
                return false;
            }

            var problem = new ProblemDetails { Status = status, Title = exception.Message };
            httpContext.Response.StatusCode = status.Value;
            await httpContext.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json", cancellationToken);

            return true;
        }
    }
}
