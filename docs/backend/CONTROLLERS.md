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

## MedicinesController
File: `backend/PharmaGo.Api/Controllers/MedicinesController.cs`

Purpose:
- exposes medicine search for mobile, bot and web clients

Endpoint:
- `GET /api/medicines/search?query=...&city=...`
  - public
  - searches by brand name, generic name or barcode
  - filters out inactive, expired and zero-availability stock
  - returns pharmacy-level availability and minimum retail price

Important details:
- uses PostgreSQL `ILIKE`
- returns only medicines that currently have positive availability

## UsersController
File: `backend/PharmaGo.Api/Controllers/UsersController.cs`

Purpose:
- gives moderators a dedicated account-management API for pharmacists and regular users

Endpoints:
- `GET /api/users`
  - moderator only
  - supports filtering by `role`, `isActive`, `pharmacyId` and `search`
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

## ReservationsController
File: `backend/PharmaGo.Api/Controllers/ReservationsController.cs`

Purpose:
- handles reservation creation, lookup and state transitions

Endpoints:
- `GET /api/reservations/my`
  - authenticated
  - returns current user reservations
- `GET /api/reservations/pharmacy/{pharmacyId}`
  - pharmacist or moderator
  - lists pharmacy reservations, optionally filtered by status
- `GET /api/reservations/{id}`
  - authenticated
  - customer can read own reservation
  - staff can read pharmacy reservations
- `POST /api/reservations`
  - authenticated
  - creates reservation against available stock in selected pharmacy
  - reserves stock immediately
- `PATCH /api/reservations/{id}/status`
  - customer can cancel own active reservation
  - staff can move reservation through pharmacy workflow

Important details:
- validates requested quantities and reservation lifetime
- reserves from the earliest-expiring stock first
- sends SignalR events on create and status changes
- publishes low-stock notifications when reservations reduce availability
- writes audit records for create and status transitions
- delegates stock release and completion rules to `IReservationStateService`

## StocksController
File: `backend/PharmaGo.Api/Controllers/StocksController.cs`

Purpose:
- allows pharmacy staff to inspect and manage inventory

Endpoints:
- `GET /api/stocks/alerts/low-stock`
  - pharmacist or moderator
  - returns low-stock items in permitted pharmacy scope
- `GET /api/stocks/pharmacy/{pharmacyId}`
  - pharmacist or moderator
  - returns stock items for a pharmacy
  - supports `lowStockOnly=true`
- `POST /api/stocks`
  - pharmacist or moderator
  - creates a new stock batch
- `PUT /api/stocks/{id}`
  - pharmacist or moderator
  - updates batch, quantity, pricing, reorder level and active state

Important details:
- pharmacists are restricted to their own pharmacy
- moderators can work across all pharmacies
- prevents duplicate `(pharmacy, medicine, batch)` records
- prevents reducing quantity below already reserved amount
- emits `stock.low` and `stock.restored` SignalR events
- writes audit records for create and update

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
