'use client'

import { useQuery } from '@tanstack/react-query'
import { reservationsApi } from './api'

export function useMyReservations() {
  return useQuery({
    queryKey: ['reservations', 'mine'],
    queryFn: () => reservationsApi.mine(),
  })
}

export function useReservationDetail(reservationId?: string) {
  return useQuery({
    queryKey: ['reservations', 'detail', reservationId],
    queryFn: () => reservationsApi.detail(reservationId!),
    enabled: Boolean(reservationId),
  })
}

export function useReservationTimeline(reservationId?: string) {
  return useQuery({
    queryKey: ['reservations', 'timeline', reservationId],
    queryFn: () => reservationsApi.timeline(reservationId!),
    enabled: Boolean(reservationId),
  })
}
