using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PharmaGo.Application.Medicines.Queries.GetMedicineRecommendations;
using PharmaGo.Domain.Models;
using PharmaGo.IntegrationTests.Infrastructure;
using PharmaGo.Infrastructure.Persistence;

namespace PharmaGo.IntegrationTests.Medicines;

public class MedicineRecommendationTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Substitutions_ShouldReturnSameGenericFormAndStrengthAlternatives()
    {
        Guid panadolId;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var panadol = await db.Medicines
                .Include(x => x.Category)
                .FirstAsync(x => x.BrandName == "Panadol");
            var northPharmacyId = await db.Pharmacies
                .Where(x => x.Name == "PharmaGo North")
                .Select(x => x.Id)
                .FirstAsync();

            panadolId = panadol.Id;

            var sameSubstitution = new Medicine
            {
                BrandName = "Paracetamol Forte",
                GenericName = "Paracetamol",
                Description = "Alternative paracetamol tablet.",
                DosageForm = "Tablet",
                Strength = "500 mg",
                Manufacturer = "Acme Pharma",
                CountryOfOrigin = "Poland",
                Barcode = "4444444444444",
                RequiresPrescription = false,
                CategoryId = panadol.CategoryId
            };

            var otherStrength = new Medicine
            {
                BrandName = "Paracetamol Kids",
                GenericName = "Paracetamol",
                Description = "Different strength paracetamol.",
                DosageForm = "Tablet",
                Strength = "250 mg",
                Manufacturer = "Acme Pharma",
                CountryOfOrigin = "Poland",
                Barcode = "5555555555555",
                RequiresPrescription = false,
                CategoryId = panadol.CategoryId
            };

            await db.Medicines.AddRangeAsync([sameSubstitution, otherStrength]);
            await db.SaveChangesAsync();

            await db.StockItems.AddRangeAsync(
            [
                new StockItem
                {
                    PharmacyId = northPharmacyId,
                    MedicineId = sameSubstitution.Id,
                    BatchNumber = "PAR-500-C1",
                    ExpirationDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(11)),
                    Quantity = 55,
                    ReservedQuantity = 0,
                    PurchasePrice = 0.95m,
                    RetailPrice = 2.10m,
                    ReorderLevel = 10,
                    IsReservable = true,
                    LastStockUpdatedAtUtc = DateTime.UtcNow,
                    ConcurrencyToken = Guid.NewGuid()
                },
                new StockItem
                {
                    PharmacyId = northPharmacyId,
                    MedicineId = otherStrength.Id,
                    BatchNumber = "PAR-250-C1",
                    ExpirationDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(11)),
                    Quantity = 60,
                    ReservedQuantity = 0,
                    PurchasePrice = 0.80m,
                    RetailPrice = 1.90m,
                    ReorderLevel = 10,
                    IsReservable = true,
                    LastStockUpdatedAtUtc = DateTime.UtcNow,
                    ConcurrencyToken = Guid.NewGuid()
                }
            ]);

            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync($"/api/medicines/{panadolId}/substitutions?limit=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadAsAsync<IReadOnlyCollection<MedicineRecommendationResponse>>();
        Assert.NotNull(payload);

        var item = Assert.Single(payload!);
        Assert.Equal("Paracetamol Forte", item.BrandName);
        Assert.Equal("Same generic name, dosage form and strength.", item.MatchReason);
    }

    [Fact]
    public async Task Similar_ShouldReturnCategoryAlternative_AndExcludeSourceMedicine()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var panadolId = await db.Medicines
            .Where(x => x.BrandName == "Panadol")
            .Select(x => x.Id)
            .FirstAsync();

        var response = await _client.GetAsync($"/api/medicines/{panadolId}/similar?limit=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadAsAsync<IReadOnlyCollection<MedicineRecommendationResponse>>();
        Assert.NotNull(payload);

        var item = Assert.Single(payload!, x => x.BrandName == "Nurofen");
        Assert.Contains("Same category", item.MatchReason);
        Assert.DoesNotContain(payload!, x => x.MedicineId == panadolId);
    }

    [Fact]
    public async Task RecommendationEndpoints_ShouldReturnNotFound_ForUnknownMedicine()
    {
        var substitutionsResponse = await _client.GetAsync($"/api/medicines/{Guid.NewGuid()}/substitutions");
        var similarResponse = await _client.GetAsync($"/api/medicines/{Guid.NewGuid()}/similar");

        Assert.Equal(HttpStatusCode.NotFound, substitutionsResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, similarResponse.StatusCode);
    }
}
