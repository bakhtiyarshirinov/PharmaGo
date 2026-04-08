'use client'

import { useQuery } from '@tanstack/react-query'
import { queryKeys } from '../../../lib/query-keys'
import { mePharmaciesApi } from './api'

export function useFavoritePharmacies(limit = 20) {
  return useQuery({
    queryKey: queryKeys.personalization.pharmacies.favorites(limit),
    queryFn: () => mePharmaciesApi.favorites(limit),
    staleTime: 30_000,
  })
}

export function useRecentPharmacies(limit = 20) {
  return useQuery({
    queryKey: queryKeys.personalization.pharmacies.recent(limit),
    queryFn: () => mePharmaciesApi.recent(limit),
    staleTime: 30_000,
  })
}
