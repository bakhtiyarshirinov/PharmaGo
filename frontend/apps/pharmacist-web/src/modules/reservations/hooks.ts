'use client'

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import type { Reservation } from '@pharmago/types'
import { browserApi } from '../../lib/api'
import { getApiErrorMessage } from '../../lib/errors'
import { queryKeys } from '../../lib/query-keys'

export function useActiveReservations(pharmacyId?: string | null) {
  return useQuery({
    queryKey: queryKeys.reservations.active(pharmacyId),
    queryFn: () => browserApi.reservations.active(pharmacyId ?? undefined),
    enabled: Boolean(pharmacyId),
  })
}

export function useReservationsByPharmacy(pharmacyId?: string | null, status?: number) {
  return useQuery({
    queryKey: queryKeys.reservations.byPharmacy(pharmacyId, status),
    queryFn: () => browserApi.reservations.byPharmacy(requireValue(pharmacyId, 'pharmacyId'), status),
    enabled: Boolean(pharmacyId),
  })
}

export function useReservationDetail(reservationId?: string) {
  return useQuery({
    queryKey: queryKeys.reservations.detail(reservationId),
    queryFn: () => browserApi.reservations.detail(requireValue(reservationId, 'reservationId')),
    enabled: Boolean(reservationId),
  })
}

export function useReservationTimeline(reservationId?: string) {
  return useQuery({
    queryKey: queryKeys.reservations.timeline(reservationId),
    queryFn: () => browserApi.reservations.timeline(requireValue(reservationId, 'reservationId')),
    enabled: Boolean(reservationId),
  })
}

function useReservationTransition(
  actionLabel: string,
  mutationFn: (reservationId: string) => Promise<Reservation>,
) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (reservationId: string) => mutationFn(reservationId),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: queryKeys.reservations.all() }),
        queryClient.invalidateQueries({ queryKey: ['dashboard'] }),
        queryClient.invalidateQueries({ queryKey: ['inventory'] }),
        queryClient.invalidateQueries({ queryKey: ['notifications'] }),
      ])

      toast.success(actionLabel)
    },
    onError: (error) => {
      toast.error(getApiErrorMessage(error, 'Не удалось обновить статус резерва.'))
    },
  })
}

export function useConfirmReservation() {
  return useReservationTransition('Резерв подтвержден.', (reservationId) => browserApi.reservations.confirm(reservationId))
}

export function useReadyForPickupReservation() {
  return useReservationTransition('Резерв переведен в статус "Готов к выдаче".', (reservationId) =>
    browserApi.reservations.readyForPickup(reservationId),
  )
}

export function useCompleteReservation() {
  return useReservationTransition('Резерв закрыт как выданный.', (reservationId) =>
    browserApi.reservations.complete(reservationId),
  )
}

export function useCancelReservation() {
  return useReservationTransition('Резерв отменен.', (reservationId) => browserApi.reservations.cancel(reservationId))
}

function requireValue(value: string | null | undefined, fieldName: string) {
  if (!value) {
    throw new Error(`${fieldName} is required`)
  }

  return value
}
