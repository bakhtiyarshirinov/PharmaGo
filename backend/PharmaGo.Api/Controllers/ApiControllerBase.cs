using Microsoft.AspNetCore.Mvc;

namespace PharmaGo.Api.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected ObjectResult ApiProblem(int statusCode, string code, string detail, string? title = null)
    {
        var problem = ApiProblemDetailsFactory.CreateProblem(statusCode, code, detail, title);
        return StatusCode(statusCode, problem);
    }

    protected BadRequestObjectResult ApiValidationProblem(string code, string detail)
    {
        var problem = ApiProblemDetailsFactory.CreateValidationProblem(code, detail);
        return BadRequest(problem);
    }

    protected ObjectResult ApiUnauthorized(string detail = "Authentication is required to access this resource.")
        => ApiProblem(StatusCodes.Status401Unauthorized, "unauthorized", detail);

    protected ObjectResult ApiForbidden(string detail = "You do not have permission to access this resource.")
        => ApiProblem(StatusCodes.Status403Forbidden, "forbidden", detail);

    protected ObjectResult ApiNotFound(string code, string detail)
        => ApiProblem(StatusCodes.Status404NotFound, code, detail);

    protected ObjectResult ApiConflict(string code, string detail)
        => ApiProblem(StatusCodes.Status409Conflict, code, detail);

    protected ObjectResult ApiUnprocessable(string code, string detail)
        => ApiProblem(StatusCodes.Status422UnprocessableEntity, code, detail);
}
