using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PharmaGo.Application.Medicines.Queries.GetMedicineDetail;
using PharmaGo.IntegrationTests.Infrastructure;
using PharmaGo.Infrastructure.Persistence;

namespace PharmaGo.IntegrationTests.Medicines;

public class MedicineCatalogTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetById_ShouldReturnMedicineCard()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var medicineId = await db.Medicines
            .Where(x => x.BrandName == "Panadol")
            .Select(x => x.Id)
            .FirstAsync();

        var response = await _client.GetAsync($"/api/medicines/{medicineId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadAsAsync<MedicineDetailResponse>();
        Assert.NotNull(payload);
        Assert.Equal("Panadol", payload!.BrandName);
        Assert.True(payload.HasAvailability);
        Assert.True(payload.PharmacyCount >= 2);
        Assert.True(payload.TotalAvailableQuantity > 0);
    }
}
