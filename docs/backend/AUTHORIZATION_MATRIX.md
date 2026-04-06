# Authorization Matrix

## Route Scope

- Public consumer read endpoints:
  - `GET /api/medicines/*`
  - `GET /api/pharmacies/*`
  - `GET /api/v1/medicines/*`
  - `GET /api/v1/pharmacies/*`
- Authenticated consumer endpoints:
  - `GET|POST|DELETE /api/me/*`
  - `GET|PUT|POST /api/notifications/*`
  - `GET /api/auth/me`
  - `POST /api/auth/logout`
  - `POST /api/auth/revoke-all`
- Reservation customer endpoints:
  - `GET /api/reservations/my`
  - `GET /api/reservations/active`
  - `GET /api/reservations/{id}`
  - `GET /api/reservations/{id}/timeline`
  - `POST /api/reservations`
  - `POST /api/reservations/{id}/cancel`
- Staff endpoints:
  - `GET /api/stocks/*`
  - `POST|PUT /api/stocks/*`
  - `GET /api/dashboard/*`
  - `GET /api/auditlogs`
  - `GET /api/reservations/pharmacy/{pharmacyId}`
  - `POST /api/reservations/{id}/confirm`
  - `POST /api/reservations/{id}/ready-for-pickup`
  - `POST /api/reservations/{id}/complete`
  - `POST /api/reservations/{id}/expire`
- Moderator endpoints:
  - `GET|POST|PUT|DELETE /api/users*`
  - `GET|POST|PUT|DELETE /api/admin/pharmacies*`
  - `PUT /api/auth/users/{id}/role`

## Role Matrix

| Area | Guest | User | Pharmacist | Moderator |
| --- | --- | --- | --- | --- |
| Public medicine/pharmacy discovery | Yes | Yes | Yes | Yes |
| Favorites, recents, notifications inbox | No | Yes | Yes | Yes |
| Create reservation | No | Yes | Yes | Yes |
| Own reservations | No | Yes | Yes | Yes |
| Pharmacy reservation operations | No | No | Yes | Yes |
| Stock alerts and stock mutations | No | No | Yes | Yes |
| Dashboard | No | No | Yes | Yes |
| Audit logs | No | No | Yes | Yes |
| User management | No | No | No | Yes |
| Pharmacy admin and schedule management | No | No | No | Yes |
| Role reassignment | No | No | No | Yes |

## Notes

- Pharmacists are pharmacy-scoped unless they also have moderator role.
- Moderators can access cross-pharmacy staff views.
- Unversioned `/api/...` routes remain supported as backward-compatible aliases.
- Versioned `/api/v1/...` routes are the canonical contract for new clients.
