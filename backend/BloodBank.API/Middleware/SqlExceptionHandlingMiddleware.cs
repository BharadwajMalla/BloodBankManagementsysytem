using Microsoft.AspNetCore.Mvc;
using BloodBank.API.Services;

namespace BloodBank.API.Middleware;

public sealed class SqlExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<SqlExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, ISqlExceptionLogger exceptionLogger)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            await exceptionLogger.LogAsync(
                exception,
                $"{context.Request.Method} {context.Request.Path}");
            logger.LogError(exception, "Unhandled request exception for {Path}", context.Request.Path);

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred."
            });
        }
    }
}
