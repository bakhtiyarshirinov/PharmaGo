import type { Reservation } from '@pharmago/types'
import { canCompleteReservation } from '../../lib/format'

export function canConfirmReservation(reservation: Reservation) {
  return reservation.status === 1
}

export function canMarkReadyForPickup(reservation: Reservation) {
  return reservation.status === 2
}

export function canCompletePickupReservation(reservation: Reservation) {
  return reservation.status === 3 && canCompleteReservation(reservation)
}

export function canCancelReservationByStaff(reservation: Reservation) {
  return reservation.status === 1 || reservation.status === 2 || reservation.status === 3
}
