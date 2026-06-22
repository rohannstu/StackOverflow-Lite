using System.Net;
using System.Text.Json;
using FluentValidation;
using StackOverflowLite.Application.Exceptions;

namespace StackOverflowLite.Host.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, response) = exception switch
        {
            ValidationException validationEx => (
                HttpStatusCode.UnprocessableEntity,
                (object)new
                {
                    status = 422,
                    title = "Validation Failed",
                    errors = validationEx.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToArray()
                        )
                }
            ),
            NotFoundException => (
                HttpStatusCode.NotFound,
                (object)new { status = 404, title = "Not Found", detail = exception.Message }
            ),
            ForbiddenException => (
                HttpStatusCode.Forbidden,
                (object)new { status = 403, title = "Forbidden", detail = exception.Message }
            ),
            BadRequestException => (
                HttpStatusCode.BadRequest,
                (object)new { status = 400, title = "Bad Request", detail = exception.Message }
            ),
            ConflictException => (
                HttpStatusCode.Conflict,
                (object)new { status = 409, title = "Conflict", detail = exception.Message }
            ),
            _ => (
                HttpStatusCode.InternalServerError,
                (object)new { status = 500, title = "Internal Server Error", detail = "An unexpected error occurred." }
            )
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception occurred");
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }
}
