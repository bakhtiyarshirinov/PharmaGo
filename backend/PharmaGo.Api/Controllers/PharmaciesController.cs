using Microsoft.AspNetCore.Mvc;
using PharmaGo.Application.Abstractions;
using PharmaGo.Application.Common.Contracts;
using PharmaGo.Application.Pharmacies.Queries.GetPharmacyDetail;
using PharmaGo.Application.Pharmacies.Queries.GetPharmacyMedicines;
using PharmaGo.Application.Pharmacies.Queries.SearchNearbyPharmacies;

namespace PharmaGo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PharmaciesController(
    IPharmacyDiscoveryService pharmacyDiscoveryService,
    IPharmacyCatalogService pharmacyCatalogService) : ControllerBase
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

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PharmacyDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PharmacyDetailResponse>> GetById(
        Guid id,
        [FromQuery] double? latitude,
        [FromQuery] double? longitude,
        CancellationToken cancellationToken)
    {
        if ((latitude.HasValue && !longitude.HasValue) ||
            (!latitude.HasValue && longitude.HasValue))
        {
            return BadRequest("Latitude and Longitude must be provided together.");
        }

        var response = await pharmacyCatalogService.GetByIdAsync(id, latitude, longitude, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpGet("{id:guid}/medicines")]
    [ProducesResponseType(typeof(PagedResponse<PharmacyMedicineResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResponse<PharmacyMedicineResponse>>> GetMedicines(
        Guid id,
        [FromQuery] GetPharmacyMedicinesRequest request,
        CancellationToken cancellationToken)
    {
        var response = await pharmacyCatalogService.GetMedicinesAsync(id, request, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }
}
