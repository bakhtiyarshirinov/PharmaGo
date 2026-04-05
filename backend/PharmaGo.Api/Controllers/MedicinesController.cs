using Microsoft.AspNetCore.Mvc;
using PharmaGo.Application.Abstractions;
using PharmaGo.Application.Medicines.Queries.GetMedicineAvailability;
using PharmaGo.Application.Medicines.Queries.GetMedicineDetail;
using PharmaGo.Application.Medicines.Queries.GetMedicineRecommendations;
using PharmaGo.Application.Medicines.Queries.SearchMedicines;

namespace PharmaGo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MedicinesController(
    IMedicineSearchService medicineSearchService,
    IMedicineCatalogService medicineCatalogService,
    IMedicineAvailabilityService medicineAvailabilityService) : ControllerBase
{
    [HttpGet("search")]
    [ProducesResponseType(typeof(IReadOnlyCollection<MedicineSearchResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyCollection<MedicineSearchResponse>>> Search(
        [FromQuery] SearchMedicinesRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return BadRequest("Search query is required.");
        }

        if ((request.Latitude.HasValue && !request.Longitude.HasValue) ||
            (!request.Latitude.HasValue && request.Longitude.HasValue))
        {
            return BadRequest("Latitude and Longitude must be provided together.");
        }

        var medicines = await medicineSearchService.SearchAsync(request, cancellationToken);
        return Ok(medicines);
    }

    [HttpGet("suggestions")]
    [ProducesResponseType(typeof(IReadOnlyCollection<MedicineSuggestionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyCollection<MedicineSuggestionResponse>>> Suggestions(
        [FromQuery] string q,
        [FromQuery] int limit,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest("Query is required.");
        }

        var response = await medicineSearchService.SuggestAsync(q, limit == 0 ? 8 : limit, cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(MedicineDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MedicineDetailResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await medicineCatalogService.GetByIdAsync(id, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpGet("{id:guid}/substitutions")]
    [ProducesResponseType(typeof(IReadOnlyCollection<MedicineRecommendationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyCollection<MedicineRecommendationResponse>>> GetSubstitutions(
        Guid id,
        [FromQuery] int limit,
        CancellationToken cancellationToken)
    {
        var response = await medicineCatalogService.GetSubstitutionsAsync(id, limit == 0 ? 10 : limit, cancellationToken);
        return response is null ? NotFound("Medicine was not found.") : Ok(response);
    }

    [HttpGet("{id:guid}/similar")]
    [ProducesResponseType(typeof(IReadOnlyCollection<MedicineRecommendationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyCollection<MedicineRecommendationResponse>>> GetSimilar(
        Guid id,
        [FromQuery] int limit,
        CancellationToken cancellationToken)
    {
        var response = await medicineCatalogService.GetSimilarAsync(id, limit == 0 ? 10 : limit, cancellationToken);
        return response is null ? NotFound("Medicine was not found.") : Ok(response);
    }

    [HttpGet("{id:guid}/availability")]
    [ProducesResponseType(typeof(MedicineAvailabilityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MedicineAvailabilityResponse>> GetAvailability(
        Guid id,
        [FromQuery] GetMedicineAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        if ((request.Latitude.HasValue && !request.Longitude.HasValue) ||
            (!request.Latitude.HasValue && request.Longitude.HasValue))
        {
            return BadRequest("Latitude and Longitude must be provided together.");
        }

        request.MedicineId = id;

        var response = await medicineAvailabilityService.GetAvailabilityAsync(request, cancellationToken);
        if (response is null)
        {
            return NotFound("Medicine was not found.");
        }

        return Ok(response);
    }
}
