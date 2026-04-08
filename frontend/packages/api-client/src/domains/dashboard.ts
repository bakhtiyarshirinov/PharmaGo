import type { DashboardRecentReservation, DashboardSummary } from '@pharmago/types'
import type { RequestOptions } from '../http/client'

export function createDashboardApi(request: <T>(path: string, options?: RequestOptions) => Promise<T>) {
  return {
    summary(pharmacyId?: string) {
      return request<DashboardSummary>('/api/v1/dashboard/summary', {
        query: pharmacyId ? { pharmacyId } : undefined,
      })
    },
    recentReservations(pharmacyId?: string) {
      return request<DashboardRecentReservation[]>('/api/v1/dashboard/recent-reservations', {
        query: pharmacyId ? { pharmacyId } : undefined,
      })
    },
  }
}
