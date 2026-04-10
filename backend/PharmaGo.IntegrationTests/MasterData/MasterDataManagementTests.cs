using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PharmaGo.Application.Auth.Contracts;
using PharmaGo.Application.Common.Contracts;
using PharmaGo.Application.MasterData.Contracts;
using PharmaGo.IntegrationTests.Infrastructure;
using PharmaGo.Infrastructure.Persistence;

namespace PharmaGo.IntegrationTests.MasterData;

public class MasterDataManagementTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Moderator_ShouldManageCategoriesAndMedicines()
    {
        var moderator = await LoginAsync("+994500000002", "Moderator123!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", moderator!.AccessToken);

        var categoryCreateResponse = await _client.PostAsJsonAsync("/api/v1/admin/master-data/categories", new CreateManagedMedicineCategoryRequest
        {
            Name = "Respiratory Care",
            Description = "Supportive respiratory medicines."
        });

        Assert.Equal(HttpStatusCode.Created, categoryCreateResponse.StatusCode);
        var category = await categoryCreateResponse.Content.ReadAsAsync<ManagedMedicineCategoryResponse>();
        Assert.NotNull(category);

        var medicineCreateResponse = await _client.PostAsJsonAsync("/api/v1/admin/master-data/medicines", new CreateManagedMedicineRequest
        {
            BrandName = "Pulmovit",
            GenericName = "Ambroxol",
            Description = "Respiratory support syrup.",
            DosageForm = "Syrup",
            Strength = "30 mg/5 ml",
            Manufacturer = "PharmaGo Labs",
            CountryOfOrigin = "Azerbaijan",
            Barcode = "9898989898989",
            RequiresPrescription = false,
            IsActive = true,
            CategoryId = category!.Id
        });

        Assert.Equal(HttpStatusCode.Created, medicineCreateResponse.StatusCode);
        var medicine = await medicineCreateResponse.Content.ReadAsAsync<ManagedMedicineResponse>();
        Assert.NotNull(medicine);
        Assert.Equal("Respiratory Care", medicine!.CategoryName);

        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/admin/master-data/medicines/{medicine.Id}", new UpdateManagedMedicineRequest
        {
            BrandName = "Pulmovit Forte",
            GenericName = "Ambroxol",
            Description = "Updated respiratory support syrup.",
            DosageForm = "Syrup",
            Strength = "30 mg/5 ml",
            Manufacturer = "PharmaGo Labs",
            CountryOfOrigin = "Azerbaijan",
            Barcode = "9898989898989",
            RequiresPrescription = false,
            IsActive = true,
            CategoryId = category.Id
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var listResponse = await _client.GetAsync("/api/admin/master-data/medicines?search=Pulmovit&page=1&pageSize=5");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var payload = await listResponse.Content.ReadAsAsync<PagedResponse<ManagedMedicineResponse>>();
        Assert.NotNull(payload);
        Assert.Contains(payload!.Items, x => x.BrandName == "Pulmovit Forte");
    }

    [Fact]
    public async Task Moderator_ShouldManageChainsDepotsAndSupplierOffers()
    {
        var moderator = await LoginAsync("+994500000002", "Moderator123!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", moderator!.AccessToken);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var medicineId = await db.Medicines.Select(x => x.Id).FirstAsync();

        var chainResponse = await _client.PostAsJsonAsync("/api/admin/master-data/pharmacy-chains", new CreateManagedPharmacyChainRequest
        {
            Name = "Baku Health Network",
            LegalName = "Baku Health Network LLC",
            SupportPhone = "+994501119988",
            SupportEmail = "network@example.com",
            IsActive = true
        });

        Assert.Equal(HttpStatusCode.Created, chainResponse.StatusCode);

        var depotResponse = await _client.PostAsJsonAsync("/api/admin/master-data/depots", new CreateManagedDepotRequest
        {
            Name = "North Logistics Hub",
            Address = "Binagadi Logistics Street 12",
            City = "Baku",
            ContactPhone = "+994501117766",
            ContactEmail = "north-hub@example.com",
            IsActive = true
        });

        Assert.Equal(HttpStatusCode.Created, depotResponse.StatusCode);
        var depot = await depotResponse.Content.ReadAsAsync<ManagedDepotResponse>();
        Assert.NotNull(depot);

        var supplierResponse = await _client.PostAsJsonAsync("/api/v1/admin/master-data/supplier-medicines", new CreateManagedSupplierMedicineRequest
        {
            DepotId = depot!.Id,
            MedicineId = medicineId,
            WholesalePrice = 4.50m,
            AvailableQuantity = 250,
            MinimumOrderQuantity = 20,
            EstimatedDeliveryHours = 12,
            IsAvailable = true
        });

        Assert.Equal(HttpStatusCode.Created, supplierResponse.StatusCode);

        var offersResponse = await _client.GetAsync($"/api/admin/master-data/supplier-medicines?depotId={depot.Id}&page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, offersResponse.StatusCode);
        var offersPayload = await offersResponse.Content.ReadAsAsync<PagedResponse<ManagedSupplierMedicineResponse>>();
        Assert.NotNull(offersPayload);
        Assert.Contains(offersPayload!.Items, x => x.DepotId == depot.Id && x.MedicineId == medicineId);
    }

    [Fact]
    public async Task Pharmacist_ShouldNotAccessMasterDataAdmin()
    {
        var pharmacist = await LoginAsync("+994500000001", "Pharmacist123!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", pharmacist!.AccessToken);

        var response = await _client.GetAsync("/api/v1/admin/master-data/categories");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Moderator_ShouldRejectMissingGenericName_WhenCreatingMedicine()
    {
        var moderator = await LoginAsync("+994500000002", "Moderator123!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", moderator!.AccessToken);

        var response = await _client.PostAsJsonAsync("/api/v1/admin/master-data/medicines", new CreateManagedMedicineRequest
        {
            BrandName = "Pulmovit",
            GenericName = "   ",
            DosageForm = "Syrup",
            Strength = "30 mg/5 ml",
            Manufacturer = "PharmaGo Labs",
            RequiresPrescription = false,
            IsActive = true
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("validation_error", problem!.Extensions["code"]?.ToString());
    }

    [Fact]
    public async Task Moderator_ShouldRejectShortDepotContactPhone()
    {
        var moderator = await LoginAsync("+994500000002", "Moderator123!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", moderator!.AccessToken);

        var response = await _client.PostAsJsonAsync("/api/admin/master-data/depots", new CreateManagedDepotRequest
        {
            Name = "Short Phone Depot",
            Address = "Logistics street 1",
            City = "Baku",
            ContactPhone = "123",
            IsActive = true
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("depot_contact_phone_invalid", problem!.Extensions["code"]?.ToString());
    }

    private async Task<AuthResponse?> LoginAsync(string phoneNumber, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            PhoneNumber = phoneNumber,
            Password = password
        });

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsAsync<AuthResponse>();
    }
}
