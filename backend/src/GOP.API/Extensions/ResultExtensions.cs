using GOP.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace GOP.API.Extensions;

public static class ResultExtensions
{
    public static IActionResult ToActionResult(this Result result)
    {
        if (result.IsSuccess)
            return new OkResult();

        return MapErrorToActionResult(result.Error);
    }

    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return new OkObjectResult(result.Value);

        return MapErrorToActionResult(result.Error);
    }

    public static IActionResult ToCreatedResult<T>(
        this Result<T> result,
        string routeName,
        object routeValues)
    {
        if (result.IsSuccess)
            return new CreatedAtRouteResult(routeName, routeValues, result.Value);

        return MapErrorToActionResult(result.Error);
    }

    private static IActionResult MapErrorToActionResult(Error error) =>
        error.Code switch
        {
            var c when c.EndsWith(".NotFound") =>
                new NotFoundObjectResult(CreateProblemDetails(404, "Resource Not Found", error.Message)),
            var c when c.Contains(".Duplicate") =>
                new ConflictObjectResult(CreateProblemDetails(409, "Conflict", error.Message)),
            var c when c.EndsWith(".Unauthorized") =>
                new ObjectResult(CreateProblemDetails(403, "Forbidden", error.Message)) { StatusCode = 403 },
            _ =>
                new UnprocessableEntityObjectResult(
                    CreateProblemDetails(422, "Validation Failed", error.Message))
        };

    private static ProblemDetails CreateProblemDetails(int status, string title, string detail) =>
        new()
        {
            Status = status,
            Title = title,
            Detail = detail,
            Type = "https://tools.ietf.org/html/rfc7807"
        };
}
