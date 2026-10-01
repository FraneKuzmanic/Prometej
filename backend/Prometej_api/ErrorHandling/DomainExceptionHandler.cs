using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Prometej_core.Exceptions;

namespace Prometej_api.ErrorHandling
{
    public sealed class DomainExceptionHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            int? status = exception switch
            {
                InvalidCredentialsException => StatusCodes.Status401Unauthorized,
                ForbiddenException => StatusCodes.Status403Forbidden,
                NotFoundException => StatusCodes.Status404NotFound,
                ConflictException => StatusCodes.Status409Conflict,
                _ => null,
            };

            // Anything else falls through to the default handler: a 500 with no message.
            if (status is null) return false;

            var problem = new ProblemDetails { Status = status, Title = exception.Message };
            httpContext.Response.StatusCode = status.Value;
            await httpContext.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json", cancellationToken);

            return true;
        }
    }
}
