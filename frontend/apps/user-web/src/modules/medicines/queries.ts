'use client'

import { useQuery } from '@tanstack/react-query'
import { browserApi } from '../../lib/api'
import type { MedicineSuggestion } from './types'

export function useMedicinesSearch(query: string) {
  return useQuery({
    queryKey: ['medicines', 'search', query],
    queryFn: () => browserApi.medicines.search(query),
    enabled: query.trim().length >= 2,
  })
}

export function useMedicineSuggestions(query: string) {
  return useQuery({
    queryKey: ['medicines', 'suggestions', query],
    queryFn: () => browserApi.medicines.suggestions(query) as Promise<MedicineSuggestion[]>,
    enabled: query.trim().length >= 2,
    staleTime: 60_000,
  })
}

export function usePopularMedicines(limit = 8) {
  return useQuery({
    queryKey: ['medicines', 'popular', limit],
    queryFn: () => browserApi.medicines.popular(limit),
    staleTime: 60_000,
  })
}

export function useMedicineDetail(medicineId?: string) {
  return useQuery({
    queryKey: ['medicines', 'detail', medicineId],
    queryFn: () => browserApi.medicines.detail(requireValue(medicineId, 'medicineId')),
    enabled: Boolean(medicineId),
  })
}

export function useMedicineAvailability(medicineId?: string) {
  return useQuery({
    queryKey: ['medicines', 'availability', medicineId],
    queryFn: () => browserApi.medicines.availability(requireValue(medicineId, 'medicineId')),
    enabled: Boolean(medicineId),
  })
}

function requireValue(value: string | undefined, fieldName: string) {
  if (!value) {
    throw new Error(`${fieldName} is required`)
  }

  return value
}
