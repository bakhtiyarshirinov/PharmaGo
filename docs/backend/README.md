# PharmaGo Backend

## Overview
PharmaGo backend is a modular `.NET 9` Web API built with a Clean Architecture-style split:

- `PharmaGo.Domain`: business entities and enums
- `PharmaGo.Application`: contracts, DTOs and abstractions used by API and infrastructure
- `PharmaGo.Infrastructure`: EF Core, PostgreSQL, JWT, seeding and internal services
- `PharmaGo.Api`: HTTP controllers, Swagger, SignalR, background workers and host bootstrapping

The backend solves four main business flows:

1. user registration and JWT authentication
2. medicine search with live availability by pharmacy
3. reservation creation and lifecycle management
4. pharmacy stock monitoring with low-stock alerts, supplier restock suggestions and realtime notifications
5. pharmacy discovery by location and medicine availability lookup

## Runtime Features
- JWT bearer authentication
- refresh tokens with rotation and revocation
- roles: `User`, `Pharmacist`, `Moderator`
- permission-based authorization policies
- PostgreSQL persistence through EF Core
- Redis-ready distributed caching with in-memory fallback
- automatic migrations on startup
- seed data for pharmacies, medicines and staff accounts
- Swagger UI in development
- SignalR hub for realtime reservation and stock events
- background worker for auto-expiring reservations
- audit log storage for sensitive business actions
- health endpoint at `/health`
- Docker Compose stack for `api + postgres + redis`

## Default Seed Accounts
- `Pharmacist`: `+994500000001` / `Pharmacist123!`
- `Moderator`: `+994500000002` / `Moderator123!`

## Main HTTP Areas
- `AuthController`: register, login, current user, role update
- `UsersController`: moderator user management, pharmacist creation, soft delete and restore
- `MedicinesController`: medicine catalog search with geo-aware ranking and pharmacy availability
- `PharmaciesController`: nearby pharmacy discovery with location-aware filters
- `ReservationsController`: create reservation, read own reservations, pharmacy reservation workflow
- `StocksController`: pharmacy stock CRUD, low-stock alerts and supplier restock suggestions
- `DashboardController`: summary metrics and recent reservations for staff dashboards
- `AuditLogsController`: staff audit trail access

## Realtime Events
SignalR hub path:

- `/hubs/notifications`

Published event names:

- `reservation.created`
- `reservation.status.changed`
- `stock.low`
- `stock.restored`

Clients are grouped automatically by:

- `user:{userId}`
- `pharmacy:{pharmacyId}`
- `role:{roleName}`
- `role:staff`

## Reservation Lifecycle
Supported statuses:

- `Pending`
- `Confirmed`
- `ReadyForPickup`
- `Completed`
- `Cancelled`
- `Expired`

Workflow rules:

- customer can cancel own active reservation
- pharmacist or moderator can move reservation through pharmacy workflow
- background worker expires overdue reservations automatically
- stock is released on `Cancelled` and `Expired`
- stock is deducted on `Completed`

## Operational Notes
- API startup runs migrations and seed logic automatically
- Swagger UI is enabled in development environment
- database health is included in `/health`
- global exception handling is enabled through ASP.NET Core `ProblemDetails`
- hot read endpoints use distributed cache versioning for safe invalidation
- if `Redis:ConnectionString` is empty, the app falls back to in-memory distributed cache

Read these next:

- `docs/backend/CONTROLLERS.md`
- `docs/backend/FILE_REFERENCE.md`
