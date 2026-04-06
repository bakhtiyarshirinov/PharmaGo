# Production Runbook

## Purpose
This runbook covers the minimum operational steps for running PharmaGo backend safely as an MVP.

## Environments
- `Development`: local development and docker-compose demo stack
- `Testing`: integration-test environment with isolated PostgreSQL and no hosted workers
- `Production`: deployment environment for real users and real data

## Required Configuration
- `ConnectionStrings:DefaultConnection`
- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:SecretKey`
- `Jwt:ExpirationMinutes`
- `RefreshToken:ExpirationDays`
- `Redis:ConnectionString`
- `Redis:InstanceName`
- `ReservationPolicy:ReservationLifetimeHours`
- `ReservationPolicy:MaxActiveReservationsPerUser`
- `ReservationExpiration:PollingIntervalSeconds`
- `ReservationNotifications:PollingIntervalSeconds`
- `ReservationNotifications:ExpiringSoonReminderMinutes`
- `DatabaseSeeding:EnableDemoData`
- `DatabaseSeeding:AllowProductionSeeding`

## Safe Production Defaults
- set `ASPNETCORE_ENVIRONMENT=Production`
- set `DatabaseSeeding:EnableDemoData=false`
- keep `DatabaseSeeding:AllowProductionSeeding=false`
- use a strong `Jwt:SecretKey`
- point `Redis:ConnectionString` to a real Redis instance when possible

## Startup Behavior
On startup the API:
1. applies EF Core migrations
2. skips demo seed unless seeding is explicitly enabled
3. starts hosted workers for reservation expiration and reservation reminders
4. exposes:
   - `/health/live`
   - `/health/ready`
   - `/health`

## Health Endpoints
- `/health/live`: process liveness only
- `/health/ready`: database and background-worker readiness
- `/health`: readiness alias for existing clients

If `/health/ready` is degraded:
- check database connectivity first
- check reservation expiration worker logs
- check reservation notification worker logs
- review the `background_workers` section in the health payload for the last successful run time

## Deploy Sequence
1. build the backend image or publish artifacts
2. back up the database
3. deploy configuration and secrets
4. start the API
5. confirm migrations applied successfully
6. verify `/health/live`
7. verify `/health/ready`
8. verify Swagger in non-production staging only
9. verify login, medicine search and reservation create with a smoke account

## Reservation Operational Checks
- confirm reservations start in `Pending`
- confirm pharmacists can `confirm`, `ready-for-pickup`, `complete`, `expire`
- confirm pharmacists cannot create reservations
- confirm reminders appear at `45`, `30` and `15` minutes before expiry
- confirm reservations expire after `2 hours`

## Notification Operational Checks
- verify `notification.received` SignalR events
- verify `/api/v1/notifications/history`
- verify `/api/v1/notifications/unread`
- verify read and bulk status actions

## Log Review Targets
- reservation creation failures
- idempotency conflicts
- stock conflicts
- notification dispatch failures
- background worker iteration failures

## Recovery Notes
- if a deployment fails after migrations, do not re-enable demo seeding
- if Redis is unavailable, the app still runs with in-memory distributed cache fallback
- if background workers fail, reservation state can still be handled manually by staff endpoints, but reminders and automatic expiry should be treated as degraded
