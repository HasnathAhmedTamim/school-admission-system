using Microsoft.AspNetCore.Diagnostics;
using SchoolAdmission.Application.Common.Exceptions;

namespace SchoolAdmission.API.Errors;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, body) = exception switch
        {
            ValidationException ex => (StatusCodes.Status400BadRequest,
                new ErrorResponse(ex.ErrorCode, ex.Message, ex.Errors.Count > 0 ? ex.Errors : null)),
            NotFoundException ex => (StatusCodes.Status404NotFound, new ErrorResponse(ex.ErrorCode, ex.Message)),
            ForbiddenException ex => (StatusCodes.Status403Forbidden, new ErrorResponse(ex.ErrorCode, ex.Message)),
            ConflictException ex => (StatusCodes.Status409Conflict, new ErrorResponse(ex.ErrorCode, ex.Message)),
            BadHttpRequestException ex => (ex.StatusCode,
                new ErrorResponse("BAD_REQUEST", "The request could not be processed. Please check the data you sent.")),
            OperationCanceledException when httpContext.RequestAborted.IsCancellationRequested => (499, null),
            _ => (StatusCodes.Status500InternalServerError,
                new ErrorResponse("INTERNAL_ERROR", "Something went wrong on our side. Please try again in a moment."))
        };

        if (statusCode >= 500)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = statusCode;
        if (body is not null)
        {
            await httpContext.Response.WriteAsJsonAsync(body, cancellationToken);
        }

        return true;
    }
}
