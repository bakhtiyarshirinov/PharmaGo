using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using PharmaGo.Application.Abstractions;
using PharmaGo.Infrastructure.Persistence;

namespace PharmaGo.IntegrationTests.Infrastructure;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string TestConnectionString =
        "Host=localhost;Port=5432;Database=pharmago_integration_tests;Username=postgres;Password=postgres";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = TestConnectionString,
                ["RefreshToken:ExpirationDays"] = "14",
                ["ReservationExpiration:PollingIntervalSeconds"] = "3600",
                ["RateLimiting:AuthPermitLimit"] = "500",
                ["RateLimiting:AuthWindowSeconds"] = "60",
                ["RateLimiting:SearchPermitLimit"] = "1000",
                ["RateLimiting:SearchWindowSeconds"] = "60",
                ["RateLimiting:ReservationCreatePermitLimit"] = "250",
                ["RateLimiting:ReservationCreateWindowSeconds"] = "60"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ApplicationDbContext>();
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IApplicationDbContext>();
            services.RemoveAll<IHostedService>();

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(TestConnectionString));

            services.AddScoped<IApplicationDbContext>(provider =>
                provider.GetRequiredService<ApplicationDbContext>());
        });
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
        await ApplicationDbContextSeeder.SeedAsync(context);
    }

    public async Task InitializeAsync()
    {
        await ResetDatabaseAsync();
    }

    public new Task DisposeAsync()
    {
        return base.DisposeAsync().AsTask();
    }
}
