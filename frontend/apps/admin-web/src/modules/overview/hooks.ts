'use client'

import { useQuery } from '@tanstack/react-query'
import { browserApi } from '../../lib/api'
import { queryKeys } from '../../lib/query-keys'

export function useAdminSummary() {
  return useQuery({
    queryKey: queryKeys.overview.summary(),
    queryFn: () => browserApi.dashboard.summary(),
  })
}

export function useAdminRecentReservations() {
  return useQuery({
    queryKey: queryKeys.overview.recentReservations(),
    queryFn: () => browserApi.dashboard.recentReservations(),
  })
}

export function useAdminUsersPreview() {
  return useQuery({
    queryKey: queryKeys.overview.usersPreview(),
    queryFn: () => browserApi.admin.users({ page: 1, pageSize: 6, sortBy: 'createdAt', sortDirection: 'desc' }),
  })
}

export function useAdminPharmaciesPreview() {
  return useQuery({
    queryKey: queryKeys.overview.pharmaciesPreview(),
    queryFn: () => browserApi.admin.pharmacies({ page: 1, pageSize: 6, sortBy: 'name', sortDirection: 'asc' }),
  })
}
