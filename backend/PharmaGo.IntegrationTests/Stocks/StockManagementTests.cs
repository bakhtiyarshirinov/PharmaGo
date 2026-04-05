using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PharmaGo.Application.Auth.Contracts;
using PharmaGo.Application.Stocks.Commands.AdjustStockQuantity;
using PharmaGo.Application.Stocks.Commands.ReceiveStock;
using PharmaGo.Application.Stocks.Commands.WriteOffStock;
using PharmaGo.Application.Stocks.Queries.GetExpiringStockAlerts;
using PharmaGo.Application.Stocks.Queries.GetOutOfStockAlerts;
using PharmaGo.Application.Stocks.Queries.GetStocks;
using PharmaGo.IntegrationTests.Infrastructure;
using PharmaGo.Infrastructure.Persistence;

namespace PharmaGo.IntegrationTests.Stocks;

public class StockManagementTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Adjust_ShouldUpdateQuantity_AndReturnUpdatedStock()
    {
        await AuthorizePharmacistAsync();

        Guid stockItemId;
        int initialQuantity;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var stockItem = await db.StockItems
                .Include(x => x.Pharmacy)
                .FirstAsync(x => x.Pharmacy!.Name == "PharmaGo Central" && x.Quantity >= 10);

            stockItemId = stockItem.Id;
            initialQuantity = stockItem.Quantity;
        }

        var response = await _client.PostAsJsonAsync($"/api/stocks/{stockItemId}/adjust", new AdjustStockQuantityRequest
        {
            QuantityDelta = -3,
            Reason = "Cycle count correction"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadAsAsync<StockItemResponse>();
        Assert.NotNull(payload);
        Assert.Equal(initialQuantity - 3, payload!.Quantity);
        Assert.NotNull(payload.LastStockUpdatedAtUtc);
    }

    [Fact]
    public async Task Receive_ShouldIncreaseQuantity_AndUpdatePricing()
    {
        await AuthorizePharmacistAsync();

        Guid stockItemId;
        int initialQuantity;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var stockItem = await db.StockItems
                .Include(x => x.Pharmacy)
                .FirstAsync(x => x.Pharmacy!.Name == "PharmaGo Central" && x.Quantity >= 5);

            stockItemId = stockItem.Id;
            initialQuantity = stockItem.Quantity;
        }

        var response = await _client.PostAsJsonAsync($"/api/stocks/{stockItemId}/receive", new ReceiveStockRequest
        {
            QuantityReceived = 12,
            PurchasePrice = 0.85m,
            RetailPrice = 1.40m,
            ReorderLevel = 8,
            Reason = "Supplier delivery"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadAsAsync<StockItemResponse>();
        Assert.NotNull(payload);
        Assert.Equal(initialQuantity + 12, payload!.Quantity);
        Assert.Equal(0.85m, payload.PurchasePrice);
        Assert.Equal(1.40m, payload.RetailPrice);
        Assert.Equal(8, payload.ReorderLevel);
    }

    [Fact]
    public async Task WriteOff_ShouldReduceAvailableStock()
    {
        await AuthorizePharmacistAsync();

        Guid stockItemId;
        int initialQuantity;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var stockItem = await db.StockItems
                .Include(x => x.Pharmacy)
                .FirstAsync(x => x.Pharmacy!.Name == "PharmaGo Central" && x.Quantity >= 6);

            stockItem.ReservedQuantity = 1;
            await db.SaveChangesAsync();

            stockItemId = stockItem.Id;
            initialQuantity = stockItem.Quantity;
        }

        var response = await _client.PostAsJsonAsync($"/api/stocks/{stockItemId}/writeoff", new WriteOffStockRequest
        {
            Quantity = 4,
            Reason = "Damaged package"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadAsAsync<StockItemResponse>();
        Assert.NotNull(payload);
        Assert.Equal(initialQuantity - 4, payload!.Quantity);
        Assert.Equal(1, payload.ReservedQuantity);
    }

    [Fact]
    public async Task WriteOff_ShouldReject_WhenQuantityExceedsAvailable()
    {
        await AuthorizePharmacistAsync();

        Guid stockItemId;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var stockItem = await db.StockItems
                .Include(x => x.Pharmacy)
                .FirstAsync(x => x.Pharmacy!.Name == "PharmaGo Central" && x.Quantity >= 3);

            stockItem.ReservedQuantity = stockItem.Quantity - 1;
            await db.SaveChangesAsync();
            stockItemId = stockItem.Id;
        }

        var response = await _client.PostAsJsonAsync($"/api/stocks/{stockItemId}/writeoff", new WriteOffStockRequest
        {
            Quantity = 2,
            Reason = "Broken seal"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Adjust_ShouldWriteRichAuditMetadata()
    {
        await AuthorizePharmacistAsync();

        Guid stockItemId;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            stockItemId = await db.StockItems
                .Include(x => x.Pharmacy)
                .Where(x => x.Pharmacy!.Name == "PharmaGo Central" && x.Quantity >= 10)
                .Select(x => x.Id)
                .FirstAsync();
        }

        var response = await _client.PostAsJsonAsync($"/api/stocks/{stockItemId}/adjust", new AdjustStockQuantityRequest
        {
            QuantityDelta = -2,
            Reason = "Cycle count correction"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var verificationScope = factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var audit = await verificationDb.AuditLogs
            .AsNoTracking()
            .Where(x => x.EntityName == "StockItem" && x.EntityId == stockItemId.ToString() && x.Action == "stock.adjusted")
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstAsync();

        Assert.NotNull(audit.MetadataJson);

        using var document = JsonDocument.Parse(audit.MetadataJson!);
        var root = document.RootElement;

        Assert.Equal("Cycle count correction", root.GetProperty("Reason").GetString());
        Assert.Equal(-2, root.GetProperty("Change").GetProperty("QuantityDelta").GetInt32());
        Assert.True(root.GetProperty("Before").GetProperty("Quantity").GetInt32() > root.GetProperty("After").GetProperty("Quantity").GetInt32());
    }

    [Fact]
    public async Task Receive_ShouldWriteBeforeAfterAuditSnapshot()
    {
        await AuthorizePharmacistAsync();

        Guid stockItemId;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            stockItemId = await db.StockItems
                .Include(x => x.Pharmacy)
                .Where(x => x.Pharmacy!.Name == "PharmaGo Central" && x.Quantity >= 5)
                .Select(x => x.Id)
                .FirstAsync();
        }

        var response = await _client.PostAsJsonAsync($"/api/stocks/{stockItemId}/receive", new ReceiveStockRequest
        {
            QuantityReceived = 10,
            PurchasePrice = 0.90m,
            RetailPrice = 1.50m,
            ReorderLevel = 9,
            Reason = "Supplier delivery"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var verificationScope = factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var audit = await verificationDb.AuditLogs
            .AsNoTracking()
            .Where(x => x.EntityName == "StockItem" && x.EntityId == stockItemId.ToString() && x.Action == "stock.received")
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstAsync();

        using var document = JsonDocument.Parse(audit.MetadataJson!);
        var root = document.RootElement;

        Assert.Equal("Supplier delivery", root.GetProperty("Reason").GetString());
        Assert.Equal(10, root.GetProperty("Change").GetProperty("QuantityDelta").GetInt32());
        Assert.Equal(1.50m, root.GetProperty("After").GetProperty("RetailPrice").GetDecimal());
        Assert.True(root.GetProperty("After").GetProperty("Quantity").GetInt32() > root.GetProperty("Before").GetProperty("Quantity").GetInt32());
    }

    [Fact]
    public async Task WriteOff_ShouldWriteReasonedAuditMetadata()
    {
        await AuthorizePharmacistAsync();

        Guid stockItemId;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var stockItem = await db.StockItems
                .Include(x => x.Pharmacy)
                .FirstAsync(x => x.Pharmacy!.Name == "PharmaGo Central" && x.Quantity >= 6);

            stockItem.ReservedQuantity = 1;
            await db.SaveChangesAsync();
            stockItemId = stockItem.Id;
        }

        var response = await _client.PostAsJsonAsync($"/api/stocks/{stockItemId}/writeoff", new WriteOffStockRequest
        {
            Quantity = 3,
            Reason = "Damaged package"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var verificationScope = factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var audit = await verificationDb.AuditLogs
            .AsNoTracking()
            .Where(x => x.EntityName == "StockItem" && x.EntityId == stockItemId.ToString() && x.Action == "stock.written_off")
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstAsync();

        using var document = JsonDocument.Parse(audit.MetadataJson!);
        var root = document.RootElement;

        Assert.Equal("Damaged package", root.GetProperty("Reason").GetString());
        Assert.Equal(-3, root.GetProperty("Change").GetProperty("QuantityDelta").GetInt32());
        Assert.Equal(1, root.GetProperty("After").GetProperty("ReservedQuantity").GetInt32());
    }

    [Fact]
    public async Task OutOfStockAlerts_ShouldReturnAggregatedMedicineGap()
    {
        await AuthorizePharmacistAsync();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var stockItems = await db.StockItems
                .Include(x => x.Pharmacy)
                .Include(x => x.Medicine)
                .Where(x => x.Pharmacy!.Name == "PharmaGo Central" && x.Medicine!.BrandName == "Panadol")
                .ToListAsync();

            foreach (var stockItem in stockItems)
            {
                stockItem.Quantity = 1;
                stockItem.ReservedQuantity = 1;
                stockItem.ExpirationDate = today.AddDays(60);
            }

            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync("/api/stocks/alerts/out-of-stock");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadAsAsync<IReadOnlyCollection<OutOfStockAlertResponse>>();
        Assert.NotNull(payload);

        var alert = Assert.Single(payload!, x => x.MedicineName == "Panadol");
        Assert.Equal("PharmaGo Central", alert.PharmacyName);
        Assert.True(alert.BatchCount >= 1);
        Assert.Equal(0, alert.TotalAvailableQuantity);
    }

    [Fact]
    public async Task ExpiringAlerts_ShouldReturnBatchesWithinRequestedWindow()
    {
        await AuthorizePharmacistAsync();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var stockItem = await db.StockItems
                .Include(x => x.Pharmacy)
                .Include(x => x.Medicine)
                .FirstAsync(x => x.Pharmacy!.Name == "PharmaGo Central" && x.Medicine!.BrandName == "Panadol");

            stockItem.Quantity = 7;
            stockItem.ReservedQuantity = 0;
            stockItem.ExpirationDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(5);
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync("/api/stocks/alerts/expiring?days=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadAsAsync<IReadOnlyCollection<ExpiringStockAlertResponse>>();
        Assert.NotNull(payload);

        var alert = Assert.Single(payload!, x => x.MedicineName == "Panadol");
        Assert.Equal("PharmaGo Central", alert.PharmacyName);
        Assert.InRange(alert.DaysUntilExpiration, 0, 10);
        Assert.Equal(7, alert.Quantity);
    }

    private async Task AuthorizePharmacistAsync()
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            PhoneNumber = "+994500000001",
            Password = "Pharmacist123!"
        });

        var auth = await loginResponse.Content.ReadAsAsync<AuthResponse>();
        Assert.NotNull(auth);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
    }
}
