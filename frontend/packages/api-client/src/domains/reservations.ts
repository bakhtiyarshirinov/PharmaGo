import type { Reservation, ReservationTimelineEvent } from '@pharmago/types'
import type { RequestOptions } from '../http/client'

export interface CreateReservationInput {
  pharmacyId: string
  notes?: string
  reserveForHours: number
  items: Array<{
    medicineId: string
    quantity: number
  }>
}

export function createReservationsApi(request: <T>(path: string, options?: RequestOptions) => Promise<T>) {
  return {
    create(input: CreateReservationInput, idempotencyKey?: string) {
      return request<Reservation>('/api/v1/reservations', {
        method: 'POST',
        body: input,
        headers: idempotencyKey ? { 'Idempotency-Key': idempotencyKey } : undefined,
      })
    },
    mine() {
      return request<Reservation[]>('/api/v1/reservations/my')
    },
    active(pharmacyId?: string) {
      return request<Reservation[]>('/api/v1/reservations/active', {
        query: pharmacyId ? { pharmacyId } : undefined,
      })
    },
    byPharmacy(pharmacyId: string, status?: number) {
      return request<Reservation[]>(`/api/v1/reservations/pharmacy/${pharmacyId}`, {
        query: status ? { status } : undefined,
      })
    },
    detail(reservationId: string) {
      return request<Reservation>(`/api/v1/reservations/${reservationId}`)
    },
    timeline(reservationId: string) {
      return request<ReservationTimelineEvent[]>(`/api/v1/reservations/${reservationId}/timeline`)
    },
    cancel(reservationId: string) {
      return request<Reservation>(`/api/v1/reservations/${reservationId}/cancel`, { method: 'POST' })
    },
    confirm(reservationId: string) {
      return request<Reservation>(`/api/v1/reservations/${reservationId}/confirm`, { method: 'POST' })
    },
    readyForPickup(reservationId: string) {
      return request<Reservation>(`/api/v1/reservations/${reservationId}/ready-for-pickup`, { method: 'POST' })
    },
    complete(reservationId: string) {
      return request<Reservation>(`/api/v1/reservations/${reservationId}/complete`, { method: 'POST' })
    },
    expire(reservationId: string) {
      return request<Reservation>(`/api/v1/reservations/${reservationId}/expire`, { method: 'POST' })
    },
  }
}
