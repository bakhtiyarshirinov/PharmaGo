using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmaGo.Application.Abstractions;
using PharmaGo.Application.Medicines.Queries.GetConsumerMedicineFeed;

namespace PharmaGo.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/me/medicines")]
[Route("api/me/medicines")]
[Authorize]
public class MeMedicinesController(
    ICurrentUserService currentUserService,
    IMedicineConsumerService medicineConsumerService) : ApiControllerBase
{
    [HttpGet("favorites")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ConsumerMedicineFeedItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ConsumerMedicineFeedItemResponse>>> GetFavorites(
        [FromQuery] int limit,
        CancellationToken cancellationToken)
    {
        if (!currentUserService.UserId.HasValue)
        {
            return ApiUnauthorized();
        }

        var response = await medicineConsumerService.GetFavoritesAsync(
            currentUserService.UserId.Value,
            limit == 0 ? 20 : limit,
            cancellationToken);

        return Ok(response);
    }

    [HttpPost("favorites/{medicineId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddFavorite(Guid medicineId, CancellationToken cancellationToken)
    {
        if (!currentUserService.UserId.HasValue)
        {
            return ApiUnauthorized();
        }

        var added = await medicineConsumerService.AddFavoriteAsync(
            currentUserService.UserId.Value,
            medicineId,
            cancellationToken);

        return added ? NoContent() : ApiNotFound("medicine_not_found", "Medicine was not found.");
    }

    [HttpDelete("favorites/{medicineId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveFavorite(Guid medicineId, CancellationToken cancellationToken)
    {
        if (!currentUserService.UserId.HasValue)
        {
            return ApiUnauthorized();
        }

        var removed = await medicineConsumerService.RemoveFavoriteAsync(
            currentUserService.UserId.Value,
            medicineId,
            cancellationToken);

        return removed ? NoContent() : ApiNotFound("medicine_not_found", "Medicine was not found.");
    }

    [HttpGet("recent")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ConsumerMedicineFeedItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ConsumerMedicineFeedItemResponse>>> GetRecent(
        [FromQuery] int limit,
        CancellationToken cancellationToken)
    {
        if (!currentUserService.UserId.HasValue)
        {
            return ApiUnauthorized();
        }

        var response = await medicineConsumerService.GetRecentAsync(
            currentUserService.UserId.Value,
            limit == 0 ? 20 : limit,
            cancellationToken);

        return Ok(response);
    }
}
