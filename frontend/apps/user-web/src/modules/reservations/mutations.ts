'use client'

import { useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import type { Reservation } from '@pharmago/types'
import { getApiErrorMessage } from '../../lib/errors'
import { queryKeys } from '../../lib/query-keys'
import { browserApi } from '../../lib/api'

export function useCreateReservation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (input: {
      pharmacyId: string
      reserveForHours: number
      notes?: string
      items: Array<{ medicineId: string; quantity: number }>
    }) => {
      const idempotencyKey = crypto.randomUUID()
      return browserApi.reservations.create(input, idempotencyKey)
    },
    onSuccess: (reservation) => {
      toast.success(`Reservation ${reservation.reservationNumber} created`)
      queryClient.setQueryData<Reservation[]>(queryKeys.reservations.mine(), (current) =>
        current ? [reservation, ...current] : [reservation],
      )
      queryClient.setQueryData(queryKeys.reservations.detail(reservation.reservationId), reservation)
      queryClient.invalidateQueries({ queryKey: queryKeys.reservations.all() })
      queryClient.invalidateQueries({ queryKey: queryKeys.notifications.all() })
    },
    onError: (error) => {
      toast.error(getApiErrorMessage(error, 'Unable to create reservation'))
    },
  })
}
