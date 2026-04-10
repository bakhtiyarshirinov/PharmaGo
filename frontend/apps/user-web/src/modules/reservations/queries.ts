'use client'

import { useQuery } from '@tanstack/react-query'
import { queryKeys } from '../../lib/query-keys'
import { reservationsApi } from './api'

export function useMyReservations() {
  return useQuery({
    queryKey: queryKeys.reservations.mine(),
    queryFn: () => reservationsApi.mine(),
  })
}

export function useReservationDetail(reservationId?: string) {
  return useQuery({
    queryKey: queryKeys.reservations.detail(reservationId),
    queryFn: () => reservationsApi.detail(requireValue(reservationId, 'reservationId')),
    enabled: Boolean(reservationId),
  })
}

export function useReservationTimeline(reservationId?: string) {
  return useQuery({
    queryKey: queryKeys.reservations.timeline(reservationId),
    queryFn: () => reservationsApi.timeline(requireValue(reservationId, 'reservationId')),
    enabled: Boolean(reservationId),
  })
}

function requireValue(value: string | undefined, fieldName: string) {
  if (!value) {
    throw new Error(`${fieldName} is required`)
  }

  return value
}
