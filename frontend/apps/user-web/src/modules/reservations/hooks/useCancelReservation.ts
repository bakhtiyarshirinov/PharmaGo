'use client'

import { useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { ApiError } from '@pharmago/api-client'
import { reservationsApi } from '../api'

export function useCancelReservation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (reservationId: string) => reservationsApi.cancel(reservationId),
    onSuccess: (reservation) => {
      toast.success(`Reservation ${reservation.reservationNumber} cancelled`)
      queryClient.invalidateQueries({ queryKey: ['reservations'] })
      queryClient.setQueryData(['reservations', 'detail', reservation.reservationId], reservation)
    },
    onError: (error) => {
      const message = error instanceof ApiError ? error.details?.detail || error.message : 'Unable to cancel reservation'
      toast.error(message)
    },
  })
}
