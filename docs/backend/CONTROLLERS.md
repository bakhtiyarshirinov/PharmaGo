# Backend Controllers

## AuthController
File: `backend/PharmaGo.Api/Controllers/AuthController.cs`

Purpose:
- handles registration, login, profile lookup and moderator role changes

Endpoints:
- `POST /api/auth/register`
  - anonymous
  - creates a new `User`
  - stores password with ASP.NET Core `PasswordHasher`
  - returns access token, refresh token and profile
- `POST /api/auth/login`
  - anonymous
  - validates phone number and password
  - returns access token, refresh token and profile
- `POST /api/auth/refresh`
  - anonymous
  - rotates refresh token and returns a new auth response
- `POST /api/auth/logout`
  - authenticated
  - revokes the provided refresh token for the current user session
- `POST /api/auth/revoke-all`
  - authenticated
  - revokes all active refresh tokens for the current user
- `GET /api/auth/me`
  - authenticated
  - returns current user profile from JWT identity
- `PUT /api/auth/users/{id}/role`
  - moderator only
  - updates user role to `User`, `Pharmacist` or `Moderator`

Important details:
- writes audit records for registration and role changes
- embeds `pharmacy_id` into JWT when user belongs to a pharmacy
- stores only hashed refresh tokens in the database
- uses refresh token rotation on every successful refresh request
- moderator role assignment is blocked in the legacy role-only endpoint to keep user management consistent

## MedicinesController
File: `backend/PharmaGo.Api/Controllers/MedicinesController.cs`

Purpose:
- exposes medicine search for mobile, bot and web clients

Endpoints:
- `GET /api/medicines/search?query=...&city=...`
  - public
  - searches by brand name, generic name or barcode
  - filters out inactive, expired and zero-availability stock
  - supports optional `latitude`, `longitude`, `radiusKm`, `openNow`, `onlyReservable`, `sortBy`, `limit` and `availabilityLimit`
  - returns pharmacy-level availability and minimum retail price
- `GET /api/medicines/suggestions?q=...`
  - public
  - returns lightweight autocomplete items for search boxes
- `GET /api/medicines/popular`
  - public
  - returns a popularity-ranked consumer feed based on reservations, favorites and current availability
- `GET /api/medicines/{id}`
  - public
  - returns full medicine card with category and current availability summary
- `GET /api/medicines/{id}/substitutions`
  - public
  - returns close brand substitutions with the same generic name, dosage form and strength
- `GET /api/medicines/{id}/similar`
  - public
  - returns similar medicines based on category, dosage form and prescription profile
- `GET /api/medicines/{id}/availability`
  - public
  - returns pharmacies that currently stock the selected medicine
  - supports `city`, `latitude`, `longitude`, `radiusKm`, `openNow`, `onlyReservable` and `sortBy`

Important details:
- uses PostgreSQL `ILIKE`
- returns only medicines that currently have positive availability
- search responses are cached and invalidated when stock or reservation state changes
- search can rank by relevance, distance or price
- nested availabilities are distance-aware and capped with `availabilityLimit` for smaller payloads
- authenticated medicine card reads automatically update the user's recent-medicines feed
- substitutions use a safe same-generic same-form same-strength rule for additive MVP behavior
- similar medicines prefer same category and dosage form while avoiding exact self matches
- availability response can sort by distance or price and includes open-now and reservation capability flags

## MeMedicinesController
File: `backend/PharmaGo.Api/Controllers/MeMedicinesController.cs`

Purpose:
- gives authenticated users consumer-specific medicine feeds and favorite actions

Endpoints:
- `GET /api/me/medicines/favorites`
  - authenticated
  - returns the user's favorite medicines with availability summary
- `POST /api/me/medicines/favorites/{medicineId}`
  - authenticated
  - idempotently adds a medicine to favorites
- `DELETE /api/me/medicines/favorites/{medicineId}`
  - authenticated
  - idempotently removes a medicine from favorites
- `GET /api/me/medicines/recent`
  - authenticated
  - returns recently viewed medicines ordered by last view time

Important details:
- favorites and recent feeds reuse the live medicine availability summary
- recent feed is driven by actual medicine-card views rather than reservation history
- favorite and recent tables are additive user-personalization storage and do not affect existing medicine contracts

## PharmaciesController
File: `backend/PharmaGo.Api/Controllers/PharmaciesController.cs`

Purpose:
- exposes location-aware pharmacy discovery for mobile, bot and web clients

Endpoint:
- `GET /api/pharmacies/search`
  - public
  - supports `query`, `city`, `latitude`, `longitude`, `radiusKm`, `openNow`, `supportsReservations`, `hasDelivery`, `page`, `pageSize`, `sortBy` and `sortDirection`
- `GET /api/pharmacies/suggestions?q=...`
  - public
  - returns lightweight autocomplete items for search boxes
- `GET /api/pharmacies/nearby-map`
  - public
  - returns lightweight map pin data
  - supports `latitude`, `longitude`, `radiusKm`, `query`, `medicineQuery`, `openNow`, `supportsReservations`, `hasDelivery` and `limit`
- `GET /api/pharmacies/popular`
  - public
  - returns a popularity-ranked consumer feed based on reservations, favorites and availability
- `GET /api/pharmacies/{id}`
  - public
  - returns pharmacy card with contacts, hours, services, support channels and stock summary
- `GET /api/pharmacies/{id}/medicines`
  - public
  - returns paged pharmacy catalog
  - supports `query`, `categoryId`, `inStockOnly`, `onlyReservable`, `page`, `pageSize`, `sortBy` and `sortDirection`

Important details:
- returns distance when client coordinates are provided
- evaluates `isOpenNow` from 24/7 flag or weekly opening-hours JSON
- authenticated pharmacy card reads automatically update the user's recent-pharmacies feed
- includes stock summary metrics per pharmacy for consumer discovery cards
- includes a dedicated lightweight map contract so frontends do not need to use the heavier search response for pins
- pharmacy catalog flow prevents search results from becoming a dead end by exposing medicine browsing inside a selected pharmacy
- uses additive geo fields and does not break existing pharmacy data contracts

## MePharmaciesController
File: `backend/PharmaGo.Api/Controllers/MePharmaciesController.cs`

Purpose:
- gives authenticated users consumer-specific pharmacy feeds and favorite actions

Endpoints:
- `GET /api/me/pharmacies/favorites`
  - authenticated
  - returns the user's favorite pharmacies with availability summary
- `POST /api/me/pharmacies/favorites/{pharmacyId}`
  - authenticated
  - idempotently adds a pharmacy to favorites
- `DELETE /api/me/pharmacies/favorites/{pharmacyId}`
  - authenticated
  - idempotently removes a pharmacy from favorites
- `GET /api/me/pharmacies/recent`
  - authenticated
  - returns recently viewed pharmacies ordered by last view time

Important details:
- favorites and recent feeds reuse live pharmacy availability summary metrics
- recent pharmacy feed is driven by actual pharmacy-card views
- favorite and recent tables are additive user-personalization storage and do not affect existing pharmacy contracts

## UsersController
File: `backend/PharmaGo.Api/Controllers/UsersController.cs`

Purpose:
- gives moderators a dedicated account-management API for pharmacists and regular users

Endpoints:
- `GET /api/users`
  - moderator only
  - supports filtering by `role`, `isActive`, `pharmacyId` and `search`
  - supports `page`, `pageSize`, `sortBy` and `sortDirection`
- `GET /api/users/{id}`
  - moderator only
  - returns a single user profile with pharmacy info
- `POST /api/users`
  - moderator only
  - creates `User` or `Pharmacist` accounts
  - rejects moderator creation through this endpoint
- `PUT /api/users/{id}`
  - moderator only
  - updates profile, role, pharmacy assignment and optional password
- `DELETE /api/users/{id}`
  - moderator only
  - soft deletes account through `IsActive = false`
- `POST /api/users/{id}/restore`
  - moderator only
  - restores a soft-deleted account

Important details:
- pharmacist accounts require a valid `PharmacyId`
- regular users cannot be assigned to a pharmacy
- moderator cannot deactivate the currently authenticated moderator account
- password changes revoke all existing refresh tokens for that user
- soft delete also revokes all active refresh tokens
- all moderator actions are written to the audit log
- list endpoint returns page metadata and sorting information

## ReservationsController
File: `backend/PharmaGo.Api/Controllers/ReservationsController.cs`

Purpose:
- handles reservation creation, lookup and state transitions

Endpoints:
- `GET /api/reservations/my`
  - authenticated
  - returns current user reservations
- `GET /api/reservations/active`
  - authenticated
  - customer sees own active reservations
  - pharmacist sees active reservations in own pharmacy by default
  - moderator can query active reservations globally or by `pharmacyId`
- `GET /api/reservations/pharmacy/{pharmacyId}`
  - pharmacist or moderator
  - lists pharmacy reservations, optionally filtered by status
- `GET /api/reservations/{id}`
  - authenticated
  - customer can read own reservation
  - staff can read pharmacy reservations
- `GET /api/reservations/{id}/timeline`
  - authenticated
  - returns reservation audit-backed lifecycle history
- `POST /api/reservations`
  - authenticated
  - creates reservation against available stock in selected pharmacy
  - reserves stock immediately
  - accepts optional `Idempotency-Key` header for safe client retries
- `POST /api/reservations/{id}/confirm`
  - pharmacist or moderator
  - explicitly confirms reservation when workflow starts from `Pending`
- `POST /api/reservations/{id}/ready-for-pickup`
  - pharmacist or moderator
  - marks reservation as prepared and ready for customer pickup
- `POST /api/reservations/{id}/complete`
  - pharmacist or moderator
  - deducts reserved stock from inventory and completes reservation
- `POST /api/reservations/{id}/cancel`
  - customer can cancel own active reservation
  - staff can cancel active pharmacy reservations
- `POST /api/reservations/{id}/expire`
  - pharmacist or moderator
  - expires reservation explicitly and releases reserved stock
- `PATCH /api/reservations/{id}/status`
  - backward-compatible generic transition endpoint
  - customer can cancel own active reservation
  - staff can move reservation through pharmacy workflow

Important details:
- validates requested quantities and reservation lifetime
- returns problem-details payloads for idempotency conflicts, stock conflicts and invalid lifecycle transitions
- reserves from the earliest-expiring stock first
- wraps reservation writes in database transactions and detects concurrent stock changes
- persists reservation create idempotency keys per user to prevent duplicate bookings on retry
- active reservations exclude already elapsed holds even if background expiration has not run yet
- timeline is built from reservation audit events and exposes actor, description and resolved status
- sends SignalR events on create and status changes
- publishes low-stock notifications when reservations reduce availability
- writes audit records for create and status transitions
- uses explicit audit actions such as `reservation.cancelled`, `reservation.completed` and `reservation.expired`
- delegates stock release and completion rules to `IReservationStateService`
- delegates transition permissions and allowed state changes to `IReservationTransitionPolicy`
- dashboard and medicine-search caches are invalidated on reservation writes and automatic expiration

## StocksController
File: `backend/PharmaGo.Api/Controllers/StocksController.cs`

Purpose:
- allows pharmacy staff to inspect and manage inventory

Endpoints:
- `GET /api/stocks/alerts/low-stock`
  - pharmacist or moderator
  - returns low-stock items in permitted pharmacy scope
- `GET /api/stocks/alerts/out-of-stock`
  - pharmacist or moderator
  - returns medicine-level out-of-stock alerts aggregated within pharmacy scope
- `GET /api/stocks/alerts/expiring`
  - pharmacist or moderator
  - returns batches expiring within the requested `days` window
- `GET /api/stocks/alerts/restock-suggestions`
  - pharmacist or moderator
  - returns supplier-backed restock suggestions for low-stock items
- `GET /api/stocks/pharmacy/{pharmacyId}`
  - pharmacist or moderator
  - returns stock items for a pharmacy
  - supports `lowStockOnly=true`
- `POST /api/stocks`
  - pharmacist or moderator
  - creates a new stock batch
- `POST /api/stocks/{id}/adjust`
  - pharmacist or moderator
  - applies signed quantity correction to an existing stock batch
- `POST /api/stocks/{id}/receive`
  - pharmacist or moderator
  - increases quantity on an existing batch and can refresh pricing or reorder level
- `POST /api/stocks/{id}/writeoff`
  - pharmacist or moderator
  - decreases only currently available stock and records the write-off reason
- `PUT /api/stocks/{id}`
  - pharmacist or moderator
  - updates batch, quantity, pricing, reorder level and active state

Important details:
- pharmacists are restricted to their own pharmacy
- moderators can work across all pharmacies
- explicit inventory commands write separate audit actions for adjustment, receiving and write-off
- write-off cannot exceed currently available non-reserved stock
- out-of-stock alerts are aggregated per pharmacy and medicine across active non-expired batches
- expiring alerts support a bounded `days` query window from 1 to 180
- prevents duplicate `(pharmacy, medicine, batch)` records
- prevents reducing quantity below already reserved amount
- returns `409 Conflict` when the stock row changed during an update
- picks the cheapest available supplier option per medicine
- respects supplier minimum order quantity and supplier availability
- emits `stock.low` and `stock.restored` SignalR events
- writes audit records for create, update and explicit stock command actions
- dashboard and medicine-search caches are invalidated on stock writes

## DashboardController
File: `backend/PharmaGo.Api/Controllers/DashboardController.cs`

Purpose:
- provides staff-facing KPI aggregates for dashboard cards and lists

Endpoints:
- `GET /api/dashboard/summary`
  - pharmacist or moderator
  - returns counts for medicines, pharmacies, users, stock, reservations and low-stock alerts
- `GET /api/dashboard/recent-reservations`
  - pharmacist or moderator
  - returns last 10 reservations within allowed scope

Important details:
- pharmacists are automatically scoped to their own pharmacy
- moderators can request global view or a specific pharmacy
- summary and recent reservation endpoints are cached

## AuditLogsController
File: `backend/PharmaGo.Api/Controllers/AuditLogsController.cs`

Purpose:
- gives staff access to the audit trail for sensitive business actions

Endpoint:
- `GET /api/auditlogs`
  - pharmacist or moderator
  - optional filters: `pharmacyId`, `entityName`, `action`
  - returns last 100 matching records in descending creation order

Important details:
- pharmacists can only view audit logs for their own pharmacy
- moderators can inspect all pharmacies

## NotificationHub
File: `backend/PharmaGo.Api/Hubs/NotificationHub.cs`

Purpose:
- SignalR hub for authenticated realtime delivery

Connection behavior:
- adds connections to user, role and pharmacy groups
- adds pharmacists and moderators to shared `role:staff` group

Authentication:
- accepts JWT bearer tokens
- WebSocket and negotiate requests can pass JWT in `access_token`
