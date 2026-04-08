'use client'

import { useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import type { Reservation } from '@pharmago/types'
import { getApiErrorMessage } from '../../../lib/errors'
import { queryKeys } from '../../../lib/query-keys'
import { reservationsApi } from '../api'

export function useCancelReservation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (reservationId: string) => reservationsApi.cancel(reservationId),
    onSuccess: (reservation) => {
      toast.success(`Reservation ${reservation.reservationNumber} cancelled`)
      queryClient.setQueryData<Reservation | undefined>(queryKeys.reservations.detail(reservation.reservationId), reservation)
      queryClient.setQueryData<Reservation[] | undefined>(queryKeys.reservations.mine(), (current) =>
        current?.map((item) => (item.reservationId === reservation.reservationId ? reservation : item)),
      )
      queryClient.invalidateQueries({ queryKey: queryKeys.reservations.all() })
      queryClient.invalidateQueries({ queryKey: queryKeys.notifications.all() })
    },
    onError: (error) => {
      toast.error(getApiErrorMessage(error, 'Unable to cancel reservation'))
    },
  })
}
