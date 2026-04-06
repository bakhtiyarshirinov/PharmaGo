# Release Checklist

## Before Launch
- verify `dotnet build backend/PharmaGo.sln -v minimal`
- verify `dotnet test backend/PharmaGo.IntegrationTests/PharmaGo.IntegrationTests.csproj -v minimal`
- confirm `ASPNETCORE_ENVIRONMENT=Production`
- confirm `DatabaseSeeding:EnableDemoData=false`
- confirm `DatabaseSeeding:AllowProductionSeeding=false`
- confirm JWT secret is not a development secret
- confirm PostgreSQL backup exists
- confirm Redis target is correct
- confirm `/health/live` is healthy
- confirm `/health/ready` is healthy

## Product Rules
- confirm pharmacists cannot create reservations
- confirm users can create reservations
- confirm max active reservations per user is `3`
- confirm reservation lifetime is `2 hours`
- confirm reminder schedule is `45`, `30`, `15` minutes
- confirm reservation completion is blocked before `PickupAvailableFromUtc`

## Admin Surfaces
- confirm moderator can manage:
  - pharmacies
  - medicine categories
  - medicines
  - pharmacy chains
  - depots
  - supplier offers
- confirm pharmacist cannot access admin master-data endpoints

## Observability
- confirm structured logs are available from the API host
- confirm reservation creation and transition logs are present
- confirm notification dispatch logs are present
- confirm background worker health payload includes worker state

## Post-Deploy Smoke
- register a user
- log in as user
- search medicines
- open a pharmacy card
- create a reservation
- confirm a reservation as pharmacist
- read notification inbox
