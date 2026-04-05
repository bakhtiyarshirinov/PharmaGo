# PharmaGo Frontend Testbed

Minimal React + Vite frontend for testing the PharmaGo backend end-to-end.

## What It Covers
- auth login/register/logout
- medicine search, popular, favorites, recent
- medicine detail, availability, substitutions, similar
- pharmacy search, popular, favorites, recent
- pharmacy detail and pharmacy catalog
- reservation create, my reservations, active reservations
- staff inventory tools for pharmacist/moderator accounts

## Run
1. Start backend:
   `dotnet run --project backend/PharmaGo.Api/PharmaGo.Api.csproj`
2. Start frontend:
   `cd frontend && npm run dev`

Default local wiring:
- frontend dev server: `http://localhost:5173`
- backend API: `http://localhost:5122`

The Vite dev server proxies `/api`, `/health` and `/hubs` to the backend by default.

Optional env:
- `VITE_API_BASE_URL`
- `VITE_PROXY_TARGET`
