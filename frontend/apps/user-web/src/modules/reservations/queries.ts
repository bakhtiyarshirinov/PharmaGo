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
    queryFn: () => reservationsApi.detail(reservationId!),
    enabled: Boolean(reservationId),
  })
}

export function useReservationTimeline(reservationId?: string) {
  return useQuery({
    queryKey: queryKeys.reservations.timeline(reservationId),
    queryFn: () => reservationsApi.timeline(reservationId!),
    enabled: Boolean(reservationId),
  })
}
