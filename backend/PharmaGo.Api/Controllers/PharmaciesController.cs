using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using PharmaGo.Application.Abstractions;
using PharmaGo.Application.Common.Contracts;
using PharmaGo.Application.Pharmacies.Queries.GetConsumerPharmacyFeed;
using PharmaGo.Application.Pharmacies.Queries.GetNearbyPharmacyMap;
using PharmaGo.Application.Pharmacies.Queries.GetPharmacyDetail;
using PharmaGo.Application.Pharmacies.Queries.GetPharmacyMedicines;
using PharmaGo.Application.Pharmacies.Queries.SearchNearbyPharmacies;
using PharmaGo.Application.Pharmacies.Queries.SuggestPharmacies;

namespace PharmaGo.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Route("api/[controller]")]
public class PharmaciesController(
    IPharmacyDiscoveryService pharmacyDiscoveryService,
    IPharmacyCatalogService pharmacyCatalogService,
    IPharmacyConsumerService pharmacyConsumerService,
    ICurrentUserService currentUserService) : ApiControllerBase
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
            return ApiValidationProblem("geo_coordinates_incomplete", "Latitude and Longitude must be provided together.");
        }

        var response = await pharmacyDiscoveryService.SearchAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpGet("suggestions")]
    [ProducesResponseType(typeof(IReadOnlyCollection<PharmacySuggestionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyCollection<PharmacySuggestionResponse>>> Suggestions(
        [FromQuery] string q,
        [FromQuery] int limit,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return ApiValidationProblem("pharmacy_suggestions_query_required", "Query is required.");
        }

        var response = await pharmacyDiscoveryService.SuggestAsync(q, limit == 0 ? 8 : limit, cancellationToken);
        return Ok(response);
    }

    [HttpGet("nearby-map")]
    [ProducesResponseType(typeof(IReadOnlyCollection<NearbyPharmacyMapResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyCollection<NearbyPharmacyMapResponse>>> NearbyMap(
        [FromQuery] GetNearbyPharmacyMapRequest request,
        CancellationToken cancellationToken)
    {
        if ((request.Latitude.HasValue && !request.Longitude.HasValue) ||
            (!request.Latitude.HasValue && request.Longitude.HasValue))
        {
            return ApiValidationProblem("geo_coordinates_incomplete", "Latitude and Longitude must be provided together.");
        }

        if (!request.Latitude.HasValue || !request.Longitude.HasValue)
        {
            return ApiValidationProblem("geo_coordinates_required", "Latitude and Longitude are required.");
        }

        var response = await pharmacyDiscoveryService.GetMapAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpGet("popular")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ConsumerPharmacyFeedItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ConsumerPharmacyFeedItemResponse>>> Popular(
        [FromQuery] int limit,
        CancellationToken cancellationToken)
    {
        var response = await pharmacyConsumerService.GetPopularAsync(
            currentUserService.UserId,
            limit == 0 ? 10 : limit,
            cancellationToken);

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
            return ApiValidationProblem("geo_coordinates_incomplete", "Latitude and Longitude must be provided together.");
        }

        var response = await pharmacyCatalogService.GetByIdAsync(id, latitude, longitude, cancellationToken);
        if (response is null)
        {
            return ApiNotFound("pharmacy_not_found", "Pharmacy was not found.");
        }

        if (currentUserService.UserId.HasValue)
        {
            await pharmacyConsumerService.RecordViewAsync(currentUserService.UserId.Value, id, cancellationToken);
        }

        return Ok(response);
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
        return response is null ? ApiNotFound("pharmacy_not_found", "Pharmacy was not found.") : Ok(response);
    }
}
