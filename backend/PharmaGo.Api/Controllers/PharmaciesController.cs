using Microsoft.AspNetCore.Mvc;
using PharmaGo.Application.Abstractions;
using PharmaGo.Application.Common.Contracts;
using PharmaGo.Application.Pharmacies.Queries.SearchNearbyPharmacies;

namespace PharmaGo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PharmaciesController(IPharmacyDiscoveryService pharmacyDiscoveryService) : ControllerBase
{
    [HttpGet("search")]
    [ProducesResponseType(typeof(PagedResponse<NearbyPharmacyResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<NearbyPharmacyResponse>>> Search(
        [FromQuery] SearchNearbyPharmaciesRequest request,
        CancellationToken cancellationToken)
    {
        if ((request.Latitude.HasValue && !request.Longitude.HasValue) ||
            (!request.Latitude.HasValue && request.Longitude.HasValue))
        {
            return BadRequest("Latitude and Longitude must be provided together.");
        }

        var response = await pharmacyDiscoveryService.SearchAsync(request, cancellationToken);
        return Ok(response);
    }
}
