using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using PharmaGo.Application.Abstractions;
using PharmaGo.Domain.Models;
using PharmaGo.Infrastructure.Auth;
using PharmaGo.Infrastructure.Caching;
using PharmaGo.Infrastructure.Persistence;
using PharmaGo.Infrastructure.Services;

namespace PharmaGo.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? throw new InvalidOperationException("JWT settings were not found.");
        var redisSettings = configuration.GetSection(RedisSettings.SectionName).Get<RedisSettings>() ?? new RedisSettings();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<RefreshTokenSettings>(configuration.GetSection(RefreshTokenSettings.SectionName));
        services.Configure<RedisSettings>(configuration.GetSection(RedisSettings.SectionName));

        if (string.IsNullOrWhiteSpace(redisSettings.ConnectionString))
        {
            services.AddDistributedMemoryCache();
        }
        else
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisSettings.ConnectionString;
                options.InstanceName = redisSettings.InstanceName;
            });
        }

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAppCacheService, DistributedAppCacheService>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IPharmacyCatalogService, PharmacyCatalogService>();
        services.AddScoped<IPharmacyConsumerService, PharmacyConsumerService>();
        services.AddScoped<IMedicineCatalogService, MedicineCatalogService>();
        services.AddScoped<IMedicineConsumerService, MedicineConsumerService>();
        services.AddScoped<IMedicineSearchService, MedicineSearchService>();
        services.AddScoped<IPharmacyDiscoveryService, PharmacyDiscoveryService>();
        services.AddScoped<IMedicineAvailabilityService, MedicineAvailabilityService>();
        services.AddScoped<INotificationInboxService, NotificationInboxService>();
        services.AddScoped<INotificationPreferenceService, NotificationPreferenceService>();
        services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
        services.AddScoped<IReservationStateService, ReservationStateService>();
        services.AddScoped<IReservationTransitionPolicy, ReservationTransitionPolicy>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        if (!string.IsNullOrWhiteSpace(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(PolicyNames.ManageUsers, policy =>
                policy.RequireClaim(RolePermissionProvider.PermissionClaimType, PermissionNames.ManageUsers));
            options.AddPolicy(PolicyNames.ManagePharmacies, policy =>
                policy.RequireClaim(RolePermissionProvider.PermissionClaimType, PermissionNames.ManagePharmacies));
            options.AddPolicy(PolicyNames.ManageOrders, policy =>
                policy.RequireClaim(RolePermissionProvider.PermissionClaimType, PermissionNames.ManageOrders));
            options.AddPolicy(PolicyNames.ManageInventory, policy =>
                policy.RequireClaim(RolePermissionProvider.PermissionClaimType, PermissionNames.ManageInventory));
            options.AddPolicy(PolicyNames.ViewDashboard, policy =>
                policy.RequireClaim(RolePermissionProvider.PermissionClaimType, PermissionNames.ViewDashboard));
            options.AddPolicy(PolicyNames.ReadAuditLogs, policy =>
                policy.RequireClaim(RolePermissionProvider.PermissionClaimType, PermissionNames.ReadAuditLogs));
            options.AddPolicy(PolicyNames.SearchMedicines, policy =>
                policy.RequireClaim(RolePermissionProvider.PermissionClaimType, PermissionNames.SearchMedicines));
            options.AddPolicy(PolicyNames.CreateReservations, policy =>
                policy.RequireClaim(RolePermissionProvider.PermissionClaimType, PermissionNames.CreateReservations));
            options.AddPolicy(PolicyNames.ReadOwnReservations, policy =>
                policy.RequireClaim(RolePermissionProvider.PermissionClaimType, PermissionNames.ReadOwnReservations));
        });

        return services;
    }
}
