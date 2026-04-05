using Microsoft.AspNetCore.Mvc;

namespace PharmaGo.Api.Controllers;

public static class ApiProblemDetailsFactory
{
    public static ProblemDetails CreateProblem(int statusCode, string code, string detail, string? title = null)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title ?? GetDefaultTitle(statusCode),
            Detail = detail,
            Type = $"https://pharmago.local/problems/{code}"
        };
        problem.Extensions["code"] = code;

        return problem;
    }

    public static ValidationProblemDetails CreateValidationProblem(string code, string detail)
    {
        var problem = new ValidationProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = GetDefaultTitle(StatusCodes.Status400BadRequest),
            Detail = detail,
            Type = $"https://pharmago.local/problems/{code}"
        };
        problem.Extensions["code"] = code;

        return problem;
    }

    private static string GetDefaultTitle(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status400BadRequest => "Request validation failed",
            StatusCodes.Status401Unauthorized => "Authentication required",
            StatusCodes.Status403Forbidden => "Access forbidden",
            StatusCodes.Status404NotFound => "Resource not found",
            StatusCodes.Status409Conflict => "Request conflicted with current state",
            StatusCodes.Status422UnprocessableEntity => "Business rule violated",
            _ => "Request failed"
        };
    }
}
