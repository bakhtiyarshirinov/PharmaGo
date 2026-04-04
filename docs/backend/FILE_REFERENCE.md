# Backend File Reference

This file documents the purpose of every backend source file currently in the repository.

## Solution Root
- `backend/PharmaGo.sln`: solution file tying API, Application, Domain and Infrastructure projects together.

## PharmaGo.Api

### Host
- `backend/PharmaGo.Api/Program.cs`: application bootstrap, dependency registration, Swagger, auth middleware, SignalR, health checks and startup database initialization.
- `backend/PharmaGo.Api/appsettings.json`: local runtime configuration for PostgreSQL, JWT and reservation expiration polling.
- `backend/PharmaGo.Api/appsettings.Development.json`: environment-specific overrides for development.
- `backend/PharmaGo.Api/PharmaGo.Api.csproj`: API project definition and NuGet dependencies.
- `backend/PharmaGo.Api/PharmaGo.Api.http`: HTTP scratch file for manual local endpoint testing.
- `backend/PharmaGo.Api/Properties/launchSettings.json`: development launch profiles and local ports.

### Controllers
- `backend/PharmaGo.Api/Controllers/AuthController.cs`: authentication and role management endpoints.
- `backend/PharmaGo.Api/Controllers/MedicinesController.cs`: public medicine search endpoints with availability projection.
- `backend/PharmaGo.Api/Controllers/ReservationsController.cs`: reservation create, read and workflow transition endpoints.
- `backend/PharmaGo.Api/Controllers/StocksController.cs`: inventory management and low-stock alert endpoints for staff.
- `backend/PharmaGo.Api/Controllers/DashboardController.cs`: dashboard summary and recent reservation endpoints for staff UI.
- `backend/PharmaGo.Api/Controllers/AuditLogsController.cs`: audit log query endpoint for staff and moderators.

### Background
- `backend/PharmaGo.Api/Background/ReservationExpirationSettings.cs`: options class for reservation expiration worker polling interval.
- `backend/PharmaGo.Api/Background/ReservationExpirationWorker.cs`: hosted service that expires overdue reservations, releases stock, writes audit logs and publishes realtime events.

### Realtime
- `backend/PharmaGo.Api/Hubs/NotificationHub.cs`: authenticated SignalR hub that groups connections by user, role and pharmacy.
- `backend/PharmaGo.Api/Realtime/NotificationEvents.cs`: centralized event name constants for realtime notifications.
- `backend/PharmaGo.Api/Realtime/RealtimeNotificationService.cs`: abstraction over SignalR hub context for reservation and stock events.

## PharmaGo.Application

### Project
- `backend/PharmaGo.Application/PharmaGo.Application.csproj`: application-layer project definition.

### Abstractions
- `backend/PharmaGo.Application/Abstractions/IApplicationDbContext.cs`: persistence contract exposed to API without depending on EF infrastructure implementation details.
- `backend/PharmaGo.Application/Abstractions/IAuditService.cs`: contract for writing audit trail records.
- `backend/PharmaGo.Application/Abstractions/ICurrentUserService.cs`: contract for reading current authenticated user identity from request context.
- `backend/PharmaGo.Application/Abstractions/IJwtTokenGenerator.cs`: contract for JWT access token generation.
- `backend/PharmaGo.Application/Abstractions/IRefreshTokenService.cs`: contract for issuing, loading and revoking refresh tokens.
- `backend/PharmaGo.Application/Abstractions/IReservationStateService.cs`: contract for releasing and completing reserved stock safely.

### Auth Contracts
- `backend/PharmaGo.Application/Auth/Contracts/AuthResponse.cs`: login and registration response with token, expiry and user profile.
- `backend/PharmaGo.Application/Auth/Contracts/LoginRequest.cs`: login payload model with validation attributes.
- `backend/PharmaGo.Application/Auth/Contracts/LogoutRequest.cs`: logout payload carrying the refresh token to revoke.
- `backend/PharmaGo.Application/Auth/Contracts/RegisterRequest.cs`: registration payload model with validation attributes.
- `backend/PharmaGo.Application/Auth/Contracts/RefreshTokenRequest.cs`: refresh payload carrying the current refresh token.
- `backend/PharmaGo.Application/Auth/Contracts/UpdateUserRoleRequest.cs`: moderator request model for changing a user role.
- `backend/PharmaGo.Application/Auth/Contracts/UserProfileResponse.cs`: normalized user profile DTO returned by auth endpoints.

### Medicines
- `backend/PharmaGo.Application/Medicines/Queries/SearchMedicines/MedicineAvailabilityDto.cs`: pharmacy-level availability row for search results.
- `backend/PharmaGo.Application/Medicines/Queries/SearchMedicines/MedicineSearchResponse.cs`: medicine search result DTO including catalog data and stock aggregates.

### Reservations
- `backend/PharmaGo.Application/Reservations/Commands/CreateReservation/CreateReservationItemRequest.cs`: item payload for reservation creation.
- `backend/PharmaGo.Application/Reservations/Commands/CreateReservation/CreateReservationRequest.cs`: reservation creation payload with pharmacy, notes, duration and items.
- `backend/PharmaGo.Application/Reservations/Commands/UpdateReservationStatus/UpdateReservationStatusRequest.cs`: reservation status transition payload.
- `backend/PharmaGo.Application/Reservations/Queries/GetReservation/ReservationItemResponse.cs`: DTO for single medicine line inside a reservation.
- `backend/PharmaGo.Application/Reservations/Queries/GetReservation/ReservationResponse.cs`: projection DTO for reservation details and list views.

### Stocks
- `backend/PharmaGo.Application/Stocks/Commands/CreateStockItem/CreateStockItemRequest.cs`: stock creation payload with validation attributes.
- `backend/PharmaGo.Application/Stocks/Commands/UpdateStockItem/UpdateStockItemRequest.cs`: stock update payload with validation attributes.
- `backend/PharmaGo.Application/Stocks/Queries/GetLowStockAlerts/LowStockAlertResponse.cs`: DTO used by low-stock APIs and realtime events.
- `backend/PharmaGo.Application/Stocks/Queries/GetStocks/StockItemResponse.cs`: inventory row DTO returned from stock endpoints.

### Dashboard
- `backend/PharmaGo.Application/Dashboard/Queries/GetDashboardSummary/DashboardSummaryResponse.cs`: KPI card DTO for staff dashboards.
- `backend/PharmaGo.Application/Dashboard/Queries/GetRecentReservations/DashboardRecentReservationResponse.cs`: compact reservation DTO for dashboard recent activity.

### Audit
- `backend/PharmaGo.Application/Audit/Queries/GetAuditLogs/AuditLogResponse.cs`: audit trail DTO returned from `AuditLogsController`.

## PharmaGo.Domain

### Project
- `backend/PharmaGo.Domain/PharmaGo.Domain.csproj`: domain-layer project definition.

### Core Models
- `backend/PharmaGo.Domain/Models/BaseEntity.cs`: shared entity base with `Id`, `CreatedAtUtc` and `UpdatedAtUtc`.
- `backend/PharmaGo.Domain/Models/AppUser.cs`: authenticated system user with role, pharmacy assignment and reservation ownership.
- `backend/PharmaGo.Domain/Models/AuditLog.cs`: immutable audit record for sensitive actions and system events.
- `backend/PharmaGo.Domain/Models/Medicine.cs`: medicine catalog entity with brand, generic, manufacturer and stock relations.
- `backend/PharmaGo.Domain/Models/MedicineCategory.cs`: medicine category or therapeutic group entity.
- `backend/PharmaGo.Domain/Models/PharmacyChain.cs`: pharmacy network root entity for grouping branches.
- `backend/PharmaGo.Domain/Models/Pharmacy.cs`: pharmacy branch entity with contact and location data.
- `backend/PharmaGo.Domain/Models/RefreshToken.cs`: persisted refresh token record with revocation and rotation metadata.
- `backend/PharmaGo.Domain/Models/Depot.cs`: wholesale depot or warehouse entity used for upstream supply.
- `backend/PharmaGo.Domain/Models/StockItem.cs`: per-batch stock record with availability, pricing and reorder logic.
- `backend/PharmaGo.Domain/Models/Reservation.cs`: reservation aggregate root with status, customer, pharmacy and reserved-until timestamp.
- `backend/PharmaGo.Domain/Models/ReservationItem.cs`: line item inside a reservation referencing medicine and quantity.
- `backend/PharmaGo.Domain/Models/SupplierMedicine.cs`: relation between depot and medicine including wholesale conditions.

### Enums
- `backend/PharmaGo.Domain/Models/Enums/ReservationStatus.cs`: allowed reservation states in the workflow.
- `backend/PharmaGo.Domain/Models/Enums/UserRole.cs`: allowed application roles: user, pharmacist and moderator.

## PharmaGo.Infrastructure

### Project
- `backend/PharmaGo.Infrastructure/PharmaGo.Infrastructure.csproj`: infrastructure-layer project definition and package references.
- `backend/PharmaGo.Infrastructure/DependencyInjection.cs`: central registration of EF Core, JWT, auth policies and internal services.

### Auth
- `backend/PharmaGo.Infrastructure/Auth/CurrentUserService.cs`: reads JWT claims from `HttpContext` and exposes current user information.
- `backend/PharmaGo.Infrastructure/Auth/JwtSettings.cs`: configuration model for issuer, audience, secret and access token lifetime.
- `backend/PharmaGo.Infrastructure/Auth/JwtTokenGenerator.cs`: creates signed JWT access tokens with role and pharmacy claims.
- `backend/PharmaGo.Infrastructure/Auth/RefreshTokenSettings.cs`: configuration model for refresh token lifetime.
- `backend/PharmaGo.Infrastructure/Auth/RoleNames.cs`: shared role and policy names used by controllers and auth setup.

### Persistence
- `backend/PharmaGo.Infrastructure/Persistence/ApplicationDbContext.cs`: EF Core DbContext implementing `IApplicationDbContext` and timestamp handling.
- `backend/PharmaGo.Infrastructure/Persistence/ApplicationDbContextSeeder.cs`: startup seed for demo catalog, pharmacies, depot and staff users.
- `backend/PharmaGo.Infrastructure/Persistence/DatabaseInitializationExtensions.cs`: startup extension that applies migrations and runs the seed.

### Entity Configurations
- `backend/PharmaGo.Infrastructure/Persistence/Configurations/AppUserConfiguration.cs`: EF mapping for users and role/storage constraints.
- `backend/PharmaGo.Infrastructure/Persistence/Configurations/AuditLogConfiguration.cs`: EF mapping for audit log table, indexes and relations.
- `backend/PharmaGo.Infrastructure/Persistence/Configurations/DepotConfiguration.cs`: EF mapping for depots.
- `backend/PharmaGo.Infrastructure/Persistence/Configurations/MedicineCategoryConfiguration.cs`: EF mapping for medicine categories.
- `backend/PharmaGo.Infrastructure/Persistence/Configurations/MedicineConfiguration.cs`: EF mapping for medicines and their constraints.
- `backend/PharmaGo.Infrastructure/Persistence/Configurations/PharmacyChainConfiguration.cs`: EF mapping for pharmacy chains.
- `backend/PharmaGo.Infrastructure/Persistence/Configurations/PharmacyConfiguration.cs`: EF mapping for pharmacies and chain relation.
- `backend/PharmaGo.Infrastructure/Persistence/Configurations/RefreshTokenConfiguration.cs`: EF mapping for stored refresh tokens and lookup indexes.
- `backend/PharmaGo.Infrastructure/Persistence/Configurations/ReservationConfiguration.cs`: EF mapping for reservation header table and indexes.
- `backend/PharmaGo.Infrastructure/Persistence/Configurations/ReservationItemConfiguration.cs`: EF mapping for reservation line items.
- `backend/PharmaGo.Infrastructure/Persistence/Configurations/StockItemConfiguration.cs`: EF mapping for inventory batches and uniqueness constraints.
- `backend/PharmaGo.Infrastructure/Persistence/Configurations/SupplierMedicineConfiguration.cs`: EF mapping for depot-medicine supply rows.

### Migrations
- `backend/PharmaGo.Infrastructure/Persistence/Migrations/20260404184117_InitialCreate.cs`: initial schema creation for the core domain model.
- `backend/PharmaGo.Infrastructure/Persistence/Migrations/20260404184117_InitialCreate.Designer.cs`: EF-generated model metadata for the initial schema migration.
- `backend/PharmaGo.Infrastructure/Persistence/Migrations/20260404185811_AddJwtAuthAndUserCredentials.cs`: schema update adding user auth fields and role support.
- `backend/PharmaGo.Infrastructure/Persistence/Migrations/20260404185811_AddJwtAuthAndUserCredentials.Designer.cs`: EF-generated metadata for the JWT/auth migration.
- `backend/PharmaGo.Infrastructure/Persistence/Migrations/20260404193239_AddAuditLogs.cs`: schema update adding persistent audit log storage.
- `backend/PharmaGo.Infrastructure/Persistence/Migrations/20260404193239_AddAuditLogs.Designer.cs`: EF-generated metadata for the audit log migration.
- `backend/PharmaGo.Infrastructure/Persistence/Migrations/20260404201759_AddRefreshTokens.cs`: schema update adding refresh token persistence.
- `backend/PharmaGo.Infrastructure/Persistence/Migrations/20260404201759_AddRefreshTokens.Designer.cs`: EF-generated metadata for the refresh token migration.
- `backend/PharmaGo.Infrastructure/Persistence/Migrations/ApplicationDbContextModelSnapshot.cs`: latest EF model snapshot used for future migration diffs.

### Services
- `backend/PharmaGo.Infrastructure/Services/AuditService.cs`: writes persisted audit records to the database.
- `backend/PharmaGo.Infrastructure/Services/RefreshTokenService.cs`: generates, hashes, rotates and revokes refresh tokens.
- `backend/PharmaGo.Infrastructure/Services/ReservationStateService.cs`: encapsulates stock release and stock deduction rules for reservation state changes.

## PharmaGo.IntegrationTests

### Project
- `backend/PharmaGo.IntegrationTests/PharmaGo.IntegrationTests.csproj`: integration test project using xUnit and ASP.NET Core test host.
- `backend/PharmaGo.IntegrationTests/AssemblyInfo.cs`: disables test parallelization to keep the shared PostgreSQL test database deterministic.

### Test Infrastructure
- `backend/PharmaGo.IntegrationTests/Infrastructure/CustomWebApplicationFactory.cs`: custom `WebApplicationFactory` that points the app to a dedicated PostgreSQL test database and resets schema between tests.
- `backend/PharmaGo.IntegrationTests/Infrastructure/JsonExtensions.cs`: shared JSON deserialization helpers for HTTP test responses.

### Test Suites
- `backend/PharmaGo.IntegrationTests/Auth/AuthFlowTests.cs`: covers register, refresh rotation, logout and revoke-all flows.
- `backend/PharmaGo.IntegrationTests/Reservations/ReservationFlowTests.cs`: covers authenticated reservation creation and pharmacist completion workflow.
