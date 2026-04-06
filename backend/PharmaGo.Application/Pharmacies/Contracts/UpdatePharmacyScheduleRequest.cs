namespace PharmaGo.Application.Pharmacies.Contracts;

public class UpdatePharmacyScheduleRequest
{
    public bool IsOpen24Hours { get; init; }
    public string? OpeningHoursJson { get; init; }
}
