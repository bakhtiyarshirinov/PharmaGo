using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using PharmaGo.Api.Auth;
using PharmaGo.Api.Background;
using PharmaGo.Api.Controllers;
using PharmaGo.Api.Hubs;
using PharmaGo.Api.OpenApi;
using PharmaGo.Api.Realtime;
using PharmaGo.Api.RateLimiting;
using PharmaGo.Api.Reservations;
using PharmaGo.Api.Services;
using PharmaGo.Application.Abstractions;
using PharmaGo.Infrastructure;
using PharmaGo.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
const string FrontendCorsPolicy = "FrontendDev";

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.Configure<RateLimitingSettings>(
    builder.Configuration.GetSection(RateLimitingSettings.SectionName));
builder.Services.Configure<ReservationPolicySettings>(
    builder.Configuration.GetSection(ReservationPolicySettings.SectionName));
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(new UrlSegmentApiVersionReader());
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'V";
    options.SubstituteApiVersionInUrl = true;
});
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var problem = ApiProblemDetailsFactory.CreateValidationProblem(
            "validation_error",
            "One or more validation errors occurred.");

        foreach (var entry in context.ModelState.Where(x => x.Value?.Errors.Count > 0))
        {
            problem.Errors[entry.Key] = entry.Value!.Errors
                .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage) ? "Invalid value." : error.ErrorMessage)
                .ToArray();
        }

        return new BadRequestObjectResult(problem);
    };
});
builder.Services.AddSignalR();
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationMiddlewareResultHandler, ApiAuthorizationMiddlewareResultHandler>();
builder.Services.AddScoped<RealtimeNotificationService>();
builder.Services.AddScoped<IReservationNotificationService, ReservationNotificationService>();
builder.Services.Configure<ReservationExpirationSettings>(
    builder.Configuration.GetSection(ReservationExpirationSettings.SectionName));
builder.Services.Configure<ReservationNotificationSettings>(
    builder.Configuration.GetSection(ReservationNotificationSettings.SectionName));
builder.Services.AddHostedService<ReservationExpirationWorker>();
builder.Services.AddHostedService<ReservationNotificationWorker>();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "http://127.0.0.1:5173",
                "http://localhost:4173",
                "http://127.0.0.1:4173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
builder.Services.AddSwaggerGen(options =>
{
    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme,
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter a valid JWT bearer token.",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = JwtBearerDefaults.AuthenticationScheme
        }
    };

    options.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [jwtSecurityScheme] = Array.Empty<string>()
    });
});
builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    await app.Services.InitializeDatabaseAsync();
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        var provider = app.Services.GetRequiredService<Asp.Versioning.ApiExplorer.IApiVersionDescriptionProvider>();
        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", $"PharmaGo API {description.GroupName.ToUpperInvariant()}");
        }
    });
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseAuthentication();
app.UseMiddleware<SelectiveRateLimitingMiddleware>(app.Configuration.GetSection(RateLimitingSettings.SectionName).Get<RateLimitingSettings>() ?? new RateLimitingSettings());
app.UseCors(FrontendCorsPolicy);
app.UseAuthorization();
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHealthChecks("/health", new HealthCheckOptions());

app.Run();

public partial class Program
{
}
