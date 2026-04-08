'use client'

import { useQuery } from '@tanstack/react-query'
import { browserApi } from '../../lib/api'

export function useMedicinesSearch(query: string) {
  return useQuery({
    queryKey: ['medicines', 'search', query],
    queryFn: () => browserApi.medicines.search(query),
    enabled: query.trim().length >= 2,
  })
}

export function useMedicineDetail(medicineId?: string) {
  return useQuery({
    queryKey: ['medicines', 'detail', medicineId],
    queryFn: () => browserApi.medicines.detail(medicineId!),
    enabled: Boolean(medicineId),
  })
}

export function useMedicineAvailability(medicineId?: string) {
  return useQuery({
    queryKey: ['medicines', 'availability', medicineId],
    queryFn: () => browserApi.medicines.availability(medicineId!),
    enabled: Boolean(medicineId),
  })
}

