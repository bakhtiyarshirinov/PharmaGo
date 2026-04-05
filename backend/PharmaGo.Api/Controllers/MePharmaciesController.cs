using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmaGo.Application.Abstractions;
using PharmaGo.Application.Pharmacies.Queries.GetConsumerPharmacyFeed;

namespace PharmaGo.Api.Controllers;

[ApiController]
[Route("api/me/pharmacies")]
[Authorize]
public class MePharmaciesController(
    ICurrentUserService currentUserService,
    IPharmacyConsumerService pharmacyConsumerService) : ApiControllerBase
{
    [HttpGet("favorites")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ConsumerPharmacyFeedItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ConsumerPharmacyFeedItemResponse>>> GetFavorites(
        [FromQuery] int limit,
        CancellationToken cancellationToken)
    {
        if (!currentUserService.UserId.HasValue)
        {
            return ApiUnauthorized();
        }

        var response = await pharmacyConsumerService.GetFavoritesAsync(
            currentUserService.UserId.Value,
            limit == 0 ? 20 : limit,
            cancellationToken);

        return Ok(response);
    }

    [HttpPost("favorites/{pharmacyId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddFavorite(Guid pharmacyId, CancellationToken cancellationToken)
    {
        if (!currentUserService.UserId.HasValue)
        {
            return ApiUnauthorized();
        }

        var added = await pharmacyConsumerService.AddFavoriteAsync(
            currentUserService.UserId.Value,
            pharmacyId,
            cancellationToken);

        return added ? NoContent() : ApiNotFound("pharmacy_not_found", "Pharmacy was not found.");
    }

    [HttpDelete("favorites/{pharmacyId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveFavorite(Guid pharmacyId, CancellationToken cancellationToken)
    {
        if (!currentUserService.UserId.HasValue)
        {
            return ApiUnauthorized();
        }

        var removed = await pharmacyConsumerService.RemoveFavoriteAsync(
            currentUserService.UserId.Value,
            pharmacyId,
            cancellationToken);

        return removed ? NoContent() : ApiNotFound("pharmacy_not_found", "Pharmacy was not found.");
    }

    [HttpGet("recent")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ConsumerPharmacyFeedItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ConsumerPharmacyFeedItemResponse>>> GetRecent(
        [FromQuery] int limit,
        CancellationToken cancellationToken)
    {
        if (!currentUserService.UserId.HasValue)
        {
            return ApiUnauthorized();
        }

        var response = await pharmacyConsumerService.GetRecentAsync(
            currentUserService.UserId.Value,
            limit == 0 ? 20 : limit,
            cancellationToken);

        return Ok(response);
    }
}
