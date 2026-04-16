using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using System.Text.Json;

namespace GOP.API.Middleware;

internal sealed class GlobalExceptionHandlerMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionHandlerMiddleware> logger,
    IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Excepción no controlada en {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = MediaTypeNames.Application.Json;

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal Server Error",
            Detail = "Error interno del servidor.",
            Type = "https://tools.ietf.org/html/rfc7807",
            Instance = context.Request.Path
        };

        if (environment.IsDevelopment())
        {
            problemDetails.Extensions["stackTrace"] = exception.ToString();
            problemDetails.Detail = exception.Message;
        }

        var traceId = context.TraceIdentifier;
        problemDetails.Extensions["traceId"] = traceId;

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return context.Response.WriteAsync(json);
    }
}
