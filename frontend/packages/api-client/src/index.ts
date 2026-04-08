import { createAdminApi } from './domains/admin'
import { createAuthApi } from './domains/auth'
import { createDashboardApi } from './domains/dashboard'
import { createMedicinesApi } from './domains/medicines'
import { createMeMedicinesApi } from './domains/me-medicines'
import { createMePharmaciesApi } from './domains/me-pharmacies'
import { createNotificationsApi } from './domains/notifications'
import { createPharmaciesApi } from './domains/pharmacies'
import { createReservationsApi } from './domains/reservations'
import { createStocksApi } from './domains/stocks'
import { createHttpClient, type HttpClientOptions } from './http/client'

export * from './http/client'
export * from './domains/stocks'

export function createApiClient(options: HttpClientOptions) {
  const request = createHttpClient(options)

  return {
    admin: createAdminApi(request),
    auth: createAuthApi(request),
    dashboard: createDashboardApi(request),
    medicines: createMedicinesApi(request),
    meMedicines: createMeMedicinesApi(request),
    mePharmacies: createMePharmaciesApi(request),
    notifications: createNotificationsApi(request),
    pharmacies: createPharmaciesApi(request),
    reservations: createReservationsApi(request),
    stocks: createStocksApi(request),
  }
}
