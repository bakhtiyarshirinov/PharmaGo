using System.ComponentModel.DataAnnotations;

namespace PharmaGo.Application.Pharmacies.Contracts;

public class UpdateManagedPharmacyRequest
{
    [Required]
    [StringLength(200)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [StringLength(400)]
    public string Address { get; init; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string City { get; init; } = string.Empty;

    [StringLength(100)]
    public string? Region { get; init; }

    [StringLength(32)]
    public string? PhoneNumber { get; init; }

    [Range(-90d, 90d)]
    public decimal? LocationLatitude { get; init; }

    [Range(-180d, 180d)]
    public decimal? LocationLongitude { get; init; }

    public bool IsOpen24Hours { get; init; }
    public string? OpeningHoursJson { get; init; }
    public bool SupportsReservations { get; init; } = true;
    public bool HasDelivery { get; init; }
    public Guid? PharmacyChainId { get; init; }
}
