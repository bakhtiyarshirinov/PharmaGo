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
- `backend/PharmaGo.Api/Controllers/MedicinesController.cs`: public medicine search, recommendation and availability endpoints.
- `backend/PharmaGo.Api/Controllers/PharmaciesController.cs`: public nearby-pharmacy discovery endpoint with geo filters and paging.
- `backend/PharmaGo.Api/Controllers/UsersController.cs`: moderator-only user management endpoints with soft delete and restore.
- `backend/PharmaGo.Api/Controllers/ReservationsController.cs`: reservation create, active/timeline lookup and explicit workflow transition endpoints.
- `backend/PharmaGo.Api/Controllers/StocksController.cs`: inventory management, explicit stock command and operational alert endpoints for staff.
- `backend/PharmaGo.Api/Controllers/DashboardController.cs`: dashboard summary and recent reservation endpoints for staff UI.
- `backend/PharmaGo.Api/Controllers/AuditLogsController.cs`: audit log query endpoint for staff and moderators.

### Background
- `backend/PharmaGo.Api/Background/ReservationExpirationSettings.cs`: options class for reservation expiration worker polling interval.
- `backend/PharmaGo.Api/Background/ReservationExpirationWorker.cs`: hosted service that expires overdue reservations, releases stock, bumps caches, writes audit logs and publishes realtime events.

### Realtime
- `backend/PharmaGo.Api/Hubs/NotificationHub.cs`: authenticated SignalR hub that groups connections by user, role and pharmacy.
- `backend/PharmaGo.Api/Realtime/NotificationEvents.cs`: centralized event name constants for realtime notifications.
- `backend/PharmaGo.Api/Realtime/RealtimeNotificationService.cs`: abstraction over SignalR hub context for reservation and stock events.

## PharmaGo.Application

### Project
- `backend/PharmaGo.Application/PharmaGo.Application.csproj`: application-layer project definition.

### Abstractions
- `backend/PharmaGo.Application/Abstractions/IApplicationDbContext.cs`: persistence contract exposed to API without depending on EF infrastructure implementation details.
- `backend/PharmaGo.Application/Abstractions/IAppCacheService.cs`: abstraction for distributed cache reads, writes and scope versioning.
- `backend/PharmaGo.Application/Abstractions/IAuditService.cs`: contract for writing audit trail records.
- `backend/PharmaGo.Application/Abstractions/ICurrentUserService.cs`: contract for reading current authenticated user identity from request context.
- `backend/PharmaGo.Application/Abstractions/IJwtTokenGenerator.cs`: contract for JWT access token generation.
- `backend/PharmaGo.Application/Abstractions/IMedicineAvailabilityService.cs`: contract for public read-model lookup of pharmacy availability for a medicine.
- `backend/PharmaGo.Application/Abstractions/IMedicineCatalogService.cs`: contract for public medicine-card lookup with aggregated availability summary.
- `backend/PharmaGo.Application/Abstractions/IMedicineSearchService.cs`: contract for consumer-facing medicine catalog search with geo-aware ranking.
- `backend/PharmaGo.Application/Abstractions/IPharmacyCatalogService.cs`: contract for pharmacy-card lookup and pharmacy-centric medicine browsing.
- `backend/PharmaGo.Application/Abstractions/IPharmacyDiscoveryService.cs`: contract for nearby-pharmacy discovery and filtering.
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

### Common
- `backend/PharmaGo.Application/Common/Contracts/PagedResponse.cs`: generic paged response wrapper with totals, pages and sorting metadata.

### Medicines
- `backend/PharmaGo.Application/Medicines/Queries/GetMedicineAvailability/GetMedicineAvailabilityRequest.cs`: query model for medicine availability lookup with geo and stock filters.
- `backend/PharmaGo.Application/Medicines/Queries/GetMedicineAvailability/MedicineAvailabilityPharmacyResponse.cs`: pharmacy row returned by medicine availability endpoint.
- `backend/PharmaGo.Application/Medicines/Queries/GetMedicineAvailability/MedicineAvailabilityResponse.cs`: aggregate medicine availability response with medicine details and pharmacy list.
- `backend/PharmaGo.Application/Medicines/Queries/GetMedicineDetail/MedicineDetailResponse.cs`: public medicine-card DTO with category and live availability summary.
- `backend/PharmaGo.Application/Medicines/Queries/GetMedicineRecommendations/MedicineRecommendationResponse.cs`: DTO for substitutions and similar-medicine recommendations with availability summary and match reason.
- `backend/PharmaGo.Application/Medicines/Queries/SearchMedicines/MedicineAvailabilityDto.cs`: pharmacy-level availability row for search results.
- `backend/PharmaGo.Application/Medicines/Queries/SearchMedicines/MedicineSuggestionResponse.cs`: lightweight medicine autocomplete row for consumer search inputs.
- `backend/PharmaGo.Application/Medicines/Queries/SearchMedicines/SearchMedicinesRequest.cs`: query model for medicine search with geo-aware ranking and payload limits.
- `backend/PharmaGo.Application/Medicines/Queries/SearchMedicines/MedicineSearchResponse.cs`: medicine search result DTO including catalog data and stock aggregates.

### Pharmacies
- `backend/PharmaGo.Application/Pharmacies/Queries/GetNearbyPharmacyMap/GetNearbyPharmacyMapRequest.cs`: query model for lightweight map-pin pharmacy discovery.
- `backend/PharmaGo.Application/Pharmacies/Queries/GetNearbyPharmacyMap/NearbyPharmacyMapResponse.cs`: lightweight pharmacy map-pin DTO with distance and matching summary.
- `backend/PharmaGo.Application/Pharmacies/Queries/GetPharmacyDetail/PharmacyDetailResponse.cs`: public pharmacy-card DTO with contacts, hours, services and stock summary.
- `backend/PharmaGo.Application/Pharmacies/Queries/GetPharmacyMedicines/GetPharmacyMedicinesRequest.cs`: query model for browsing medicines inside a pharmacy.
- `backend/PharmaGo.Application/Pharmacies/Queries/GetPharmacyMedicines/PharmacyMedicineResponse.cs`: pharmacy-centric medicine row with availability and pricing summary.
- `backend/PharmaGo.Application/Pharmacies/Queries/SearchNearbyPharmacies/NearbyPharmacyResponse.cs`: paged pharmacy discovery row including distance, availability summary and operating flags.
- `backend/PharmaGo.Application/Pharmacies/Queries/SearchNearbyPharmacies/SearchNearbyPharmaciesRequest.cs`: query model for nearby-pharmacy search and paging.
- `backend/PharmaGo.Application/Pharmacies/Queries/SuggestPharmacies/PharmacySuggestionResponse.cs`: lightweight pharmacy autocomplete row for consumer search inputs.

### Reservations
- `backend/PharmaGo.Application/Reservations/Commands/CreateReservation/CreateReservationItemRequest.cs`: item payload for reservation creation.
- `backend/PharmaGo.Application/Reservations/Commands/CreateReservation/CreateReservationRequest.cs`: reservation creation payload with pharmacy, notes, duration and items.
- `backend/PharmaGo.Application/Reservations/Commands/UpdateReservationStatus/UpdateReservationStatusRequest.cs`: reservation status transition payload.
- `backend/PharmaGo.Application/Reservations/Queries/GetReservation/ReservationItemResponse.cs`: DTO for single medicine line inside a reservation.
- `backend/PharmaGo.Application/Reservations/Queries/GetReservation/ReservationResponse.cs`: projection DTO for reservation details, active lists and lifecycle timestamps.
- `backend/PharmaGo.Application/Reservations/Queries/GetReservationTimeline/ReservationTimelineEventResponse.cs`: audit-backed reservation timeline event DTO with actor and resolved status.

### Stocks
- `backend/PharmaGo.Application/Stocks/Commands/AdjustStockQuantity/AdjustStockQuantityRequest.cs`: signed quantity-correction payload for stock adjustments.
- `backend/PharmaGo.Application/Stocks/Commands/CreateStockItem/CreateStockItemRequest.cs`: stock creation payload with validation attributes.
- `backend/PharmaGo.Application/Stocks/Commands/ReceiveStock/ReceiveStockRequest.cs`: stock receiving payload with quantity and optional pricing updates.
- `backend/PharmaGo.Application/Stocks/Commands/UpdateStockItem/UpdateStockItemRequest.cs`: stock update payload with validation attributes.
- `backend/PharmaGo.Application/Stocks/Commands/WriteOffStock/WriteOffStockRequest.cs`: stock write-off payload with required quantity and optional reason.
- `backend/PharmaGo.Application/Stocks/Queries/GetExpiringStockAlerts/ExpiringStockAlertResponse.cs`: DTO for near-expiry stock batch alerts.
- `backend/PharmaGo.Application/Stocks/Queries/GetLowStockAlerts/LowStockAlertResponse.cs`: DTO used by low-stock APIs and realtime events.
- `backend/PharmaGo.Application/Stocks/Queries/GetOutOfStockAlerts/OutOfStockAlertResponse.cs`: medicine-level DTO for out-of-stock alerts aggregated by pharmacy.
- `backend/PharmaGo.Application/Stocks/Queries/GetStocks/StockItemResponse.cs`: inventory row DTO returned from stock endpoints.

### Dashboard
- `backend/PharmaGo.Application/Dashboard/Queries/GetDashboardSummary/DashboardSummaryResponse.cs`: KPI card DTO for staff dashboards.
- `backend/PharmaGo.Application/Dashboard/Queries/GetRecentReservations/DashboardRecentReservationResponse.cs`: compact reservation DTO for dashboard recent activity.

### Audit
- `backend/PharmaGo.Application/Audit/Queries/GetAuditLogs/AuditLogResponse.cs`: audit trail DTO returned from `AuditLogsController`.

### Users
- `backend/PharmaGo.Application/Users/Contracts/CreateManagedUserRequest.cs`: moderator payload for creating a user or pharmacist account.
- `backend/PharmaGo.Application/Users/Contracts/UpdateManagedUserRequest.cs`: moderator payload for editing account data, role and optional password.
- `backend/PharmaGo.Application/Users/Contracts/UserManagementResponse.cs`: moderator-facing user DTO with active state and pharmacy display info.

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
- `backend/PharmaGo.Domain/Models/Reservation.cs`: reservation aggregate root with status, customer, pharmacy and reserved-until timestamp.
- `backend/PharmaGo.Domain/Models/ReservationItem.cs`: line item inside a reservation referencing medicine and quantity.
- `backend/PharmaGo.Domain/Models/SupplierMedicine.cs`: relation between depot and medicine including wholesale conditions.
- `backend/PharmaGo.Domain/Models/StockItem.cs`: per-batch stock record with availability, pricing, reorder logic and optimistic concurrency token.

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
- `backend/PharmaGo.Infrastructure/Auth/PermissionNames.cs`: central permission list used by authorization policies.
- `backend/PharmaGo.Infrastructure/Auth/PolicyNames.cs`: shared policy-name constants used by controllers.
- `backend/PharmaGo.Infrastructure/Auth/RefreshTokenSettings.cs`: configuration model for refresh token lifetime.
- `backend/PharmaGo.Infrastructure/Auth/RolePermissionProvider.cs`: maps user roles to permission claims embedded into JWTs.
- `backend/PharmaGo.Infrastructure/Auth/RoleNames.cs`: shared role and policy names used by controllers and auth setup.

### Caching
- `backend/PharmaGo.Infrastructure/Caching/CacheScopes.cs`: named cache scopes used for version-based invalidation.
- `backend/PharmaGo.Infrastructure/Caching/DistributedAppCacheService.cs`: distributed cache adapter with JSON serialization and scope-version support.
- `backend/PharmaGo.Infrastructure/Caching/RedisSettings.cs`: configuration model for Redis connection and instance naming.

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
- `backend/PharmaGo.Infrastructure/Persistence/Migrations/20260404215832_AddDiscoverySchemaSupport.cs`: schema update adding discovery-friendly pharmacy and stock fields plus search indexes.
- `backend/PharmaGo.Infrastructure/Persistence/Migrations/20260404215832_AddDiscoverySchemaSupport.Designer.cs`: EF-generated metadata for the discovery schema migration.
- `backend/PharmaGo.Infrastructure/Persistence/Migrations/20260404222137_AddReservationAndStockConcurrency.cs`: schema update adding optimistic concurrency tokens for reservations and stock rows.
- `backend/PharmaGo.Infrastructure/Persistence/Migrations/20260404222137_AddReservationAndStockConcurrency.Designer.cs`: EF-generated metadata for the reservation/stock concurrency migration.
- `backend/PharmaGo.Infrastructure/Persistence/Migrations/20260405145954_AddReservationLifecycleTracking.cs`: schema update adding reservation lifecycle timestamps for ready, complete and expire states.
- `backend/PharmaGo.Infrastructure/Persistence/Migrations/20260405145954_AddReservationLifecycleTracking.Designer.cs`: EF-generated metadata for the reservation lifecycle migration.
- `backend/PharmaGo.Infrastructure/Persistence/Migrations/ApplicationDbContextModelSnapshot.cs`: latest EF model snapshot used for future migration diffs.

### Services
- `backend/PharmaGo.Infrastructure/Services/AuditService.cs`: writes persisted audit records to the database.
- `backend/PharmaGo.Infrastructure/Services/MedicineAvailabilityService.cs`: builds consumer-facing pharmacy availability read models for a selected medicine.
- `backend/PharmaGo.Infrastructure/Services/MedicineCatalogService.cs`: builds public medicine-card, substitution and similar-medicine read models with cached availability summaries.
- `backend/PharmaGo.Infrastructure/Services/MedicineSearchService.cs`: executes consumer-facing medicine search with geo filters, ranking and capped nested availabilities.
- `backend/PharmaGo.Infrastructure/Services/PharmacyCatalogService.cs`: builds pharmacy-card and pharmacy-centric medicine catalog read models.
- `backend/PharmaGo.Infrastructure/Services/PharmacyDiscoveryService.cs`: searches nearby pharmacies with geo, opening-hours and availability summary calculations.
- `backend/PharmaGo.Infrastructure/Services/PharmacyDiscoverySupport.cs`: shared helpers for distance calculation and opening-hours evaluation.
- `backend/PharmaGo.Infrastructure/Services/RefreshTokenService.cs`: generates, hashes, rotates and revokes refresh tokens.
- `backend/PharmaGo.Infrastructure/Services/ReservationStateService.cs`: encapsulates stock release and stock deduction rules for reservation state changes and fails fast on inconsistent reserved quantities.

## PharmaGo.IntegrationTests

### Project
- `backend/PharmaGo.IntegrationTests/PharmaGo.IntegrationTests.csproj`: integration test project using xUnit and ASP.NET Core test host.
- `backend/PharmaGo.IntegrationTests/AssemblyInfo.cs`: disables test parallelization to keep the shared PostgreSQL test database deterministic.

### Test Infrastructure
- `backend/PharmaGo.IntegrationTests/Infrastructure/CustomWebApplicationFactory.cs`: custom `WebApplicationFactory` that points the app to a dedicated PostgreSQL test database and resets schema between tests.
- `backend/PharmaGo.IntegrationTests/Infrastructure/JsonExtensions.cs`: shared JSON deserialization helpers for HTTP test responses.

### Test Suites
- `backend/PharmaGo.IntegrationTests/Auth/AuthFlowTests.cs`: covers register, refresh rotation, logout and revoke-all flows.
- `backend/PharmaGo.IntegrationTests/Medicines/MedicineAvailabilityTests.cs`: covers consumer-facing medicine availability lookup and reservable-only filtering.
- `backend/PharmaGo.IntegrationTests/Medicines/MedicineCatalogTests.cs`: covers medicine-card lookup with live summary data.
- `backend/PharmaGo.IntegrationTests/Medicines/MedicineRecommendationTests.cs`: covers substitution and similar-medicine recommendation endpoints.
- `backend/PharmaGo.IntegrationTests/Medicines/MedicineSearchTests.cs`: covers geo-aware medicine search, reservable-only filtering and invalid coordinate input handling.
- `backend/PharmaGo.IntegrationTests/Medicines/MedicineSuggestionsTests.cs`: covers lightweight medicine autocomplete endpoints.
- `backend/PharmaGo.IntegrationTests/Pharmacies/PharmacyCatalogTests.cs`: covers pharmacy-card lookup and pharmacy-centric medicine catalog browsing.
- `backend/PharmaGo.IntegrationTests/Pharmacies/PharmacyDiscoveryTests.cs`: covers nearby-pharmacy discovery, open-now filtering and invalid geo input handling.
- `backend/PharmaGo.IntegrationTests/Pharmacies/PharmacySuggestionsTests.cs`: covers pharmacy autocomplete and lightweight nearby-map pin endpoints.
- `backend/PharmaGo.IntegrationTests/Reservations/ReservationFlowTests.cs`: covers authenticated reservation creation, explicit lifecycle commands, active/timeline reads and concurrent reservation hardening.
- `backend/PharmaGo.IntegrationTests/Stocks/StockManagementTests.cs`: covers explicit stock adjust/receive/writeoff commands and operational stock alerts.
- `backend/PharmaGo.IntegrationTests/Users/UserManagementTests.cs`: covers moderator account creation, soft delete and restore scenarios.

## Runtime Tooling
- `backend/PharmaGo.Api/Dockerfile`: multistage .NET 9 container build for the API.
- `docker-compose.yml`: local orchestration for API, PostgreSQL and Redis.
- `.env.example`: sample environment variables for local Docker Compose runs.
- `.dockerignore`: excludes build artifacts and unnecessary files from Docker build context.
