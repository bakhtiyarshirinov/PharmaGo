'use client'

import { useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { ApiError } from '@pharmago/api-client'
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
      queryClient.invalidateQueries({ queryKey: ['reservations'] })
      queryClient.invalidateQueries({ queryKey: ['notifications', 'unread'] })
    },
    onError: (error) => {
      const message = error instanceof ApiError ? error.details?.detail || error.message : 'Unable to create reservation'
      toast.error(message)
    },
  })
}
