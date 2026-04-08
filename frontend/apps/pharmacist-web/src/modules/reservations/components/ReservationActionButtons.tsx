'use client'

import { Button } from '@pharmago/ui'
import type { Reservation } from '@pharmago/types'
import { getCompleteGuardMessage } from '../../../lib/format'
import {
  useCancelReservation,
  useCompleteReservation,
  useConfirmReservation,
  useReadyForPickupReservation,
} from '../hooks'
import {
  canCancelReservationByStaff,
  canCompletePickupReservation,
  canConfirmReservation,
  canMarkReadyForPickup,
} from '../policy'

interface ReservationActionButtonsProps {
  reservation: Reservation
  compact?: boolean
}

export function ReservationActionButtons({ reservation, compact = false }: ReservationActionButtonsProps) {
  const confirm = useConfirmReservation()
  const ready = useReadyForPickupReservation()
  const complete = useCompleteReservation()
  const cancel = useCancelReservation()
  const completeGuardMessage = getCompleteGuardMessage(reservation)
  const isBusy =
    confirm.isPending ||
    ready.isPending ||
    complete.isPending ||
    cancel.isPending

  return (
    <div className="flex flex-wrap items-center gap-2">
      {canConfirmReservation(reservation) ? (
        <Button
          size={compact ? 'sm' : 'md'}
          onClick={() => confirm.mutate(reservation.reservationId)}
          disabled={isBusy}
        >
          Подтвердить
        </Button>
      ) : null}

      {canMarkReadyForPickup(reservation) ? (
        <Button
          size={compact ? 'sm' : 'md'}
          variant="secondary"
          onClick={() => ready.mutate(reservation.reservationId)}
          disabled={isBusy}
        >
          Готов к выдаче
        </Button>
      ) : null}

      {reservation.status === 3 ? (
        <Button
          size={compact ? 'sm' : 'md'}
          variant="outline"
          onClick={() => complete.mutate(reservation.reservationId)}
          disabled={isBusy || !canCompletePickupReservation(reservation)}
          title={completeGuardMessage ?? undefined}
        >
          Выдать
        </Button>
      ) : null}

      {canCancelReservationByStaff(reservation) ? (
        <Button
          size={compact ? 'sm' : 'md'}
          variant="destructive"
          onClick={() => cancel.mutate(reservation.reservationId)}
          disabled={isBusy}
        >
          Отменить
        </Button>
      ) : null}
    </div>
  )
}
