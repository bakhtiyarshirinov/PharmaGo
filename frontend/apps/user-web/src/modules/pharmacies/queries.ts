'use client'

import { useQuery } from '@tanstack/react-query'
import { pharmaciesApi } from './api'
import type { PharmacySearchFilters, PharmacySuggestion } from './types'

export function usePharmaciesSearch(filters: PharmacySearchFilters) {
  return useQuery({
    queryKey: ['pharmacies', 'search', filters],
    queryFn: () =>
      pharmaciesApi.search({
        query: filters.query,
        latitude: filters.latitude,
        longitude: filters.longitude,
        page: filters.page,
        pageSize: filters.pageSize,
      }),
  })
}

export function usePharmacySuggestions(query: string) {
  return useQuery({
    queryKey: ['pharmacies', 'suggestions', query],
    queryFn: () => pharmaciesApi.suggestions(query) as Promise<PharmacySuggestion[]>,
    enabled: query.trim().length >= 2,
    staleTime: 60_000,
  })
}

export function usePopularPharmacies(limit = 6) {
  return useQuery({
    queryKey: ['pharmacies', 'popular', limit],
    queryFn: () => pharmaciesApi.popular(limit),
  })
}

export function usePharmacyDetail(pharmacyId?: string) {
  return useQuery({
    queryKey: ['pharmacies', 'detail', pharmacyId],
    queryFn: () => pharmaciesApi.detail(pharmacyId!),
    enabled: Boolean(pharmacyId),
  })
}

export function usePharmacyMedicines(pharmacyId?: string, page = 1, pageSize = 12) {
  return useQuery({
    queryKey: ['pharmacies', 'medicines', pharmacyId, page, pageSize],
    queryFn: () => pharmaciesApi.medicines(pharmacyId!, page, pageSize),
    enabled: Boolean(pharmacyId),
  })
}
