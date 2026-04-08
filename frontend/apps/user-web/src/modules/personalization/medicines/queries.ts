'use client'

import { useQuery } from '@tanstack/react-query'
import { queryKeys } from '../../../lib/query-keys'
import { meMedicinesApi } from './api'

export function useFavoriteMedicines(limit = 20) {
  return useQuery({
    queryKey: queryKeys.personalization.medicines.favorites(limit),
    queryFn: () => meMedicinesApi.favorites(limit),
    staleTime: 30_000,
  })
}

export function useRecentMedicines(limit = 20) {
  return useQuery({
    queryKey: queryKeys.personalization.medicines.recent(limit),
    queryFn: () => meMedicinesApi.recent(limit),
    staleTime: 30_000,
  })
}
