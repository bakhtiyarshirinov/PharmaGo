using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace PharmaGo.Infrastructure.Persistence;

public static class DatabaseInitializationExtensions
{
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        var seedSettings = scope.ServiceProvider.GetService<IOptions<DatabaseSeedSettings>>()?.Value ?? new DatabaseSeedSettings();

        if (context.Database.IsNpgsql())
        {
            await context.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            await context.Database.EnsureCreatedAsync(cancellationToken);
        }

        if (seedSettings.EnableDemoData && (!environment.IsProduction() || seedSettings.AllowProductionSeeding))
        {
            await ApplicationDbContextSeeder.SeedAsync(context, cancellationToken);
        }
    }
}
