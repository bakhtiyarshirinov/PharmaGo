import { apiRequest } from './client'

export function createReservation(payload) {
  return apiRequest('/api/reservations', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function getMyReservations() {
  return apiRequest('/api/reservations/my')
}

export function getActiveReservations() {
  return apiRequest('/api/reservations/active')
}

export function getReservationTimeline(id) {
  return apiRequest(`/api/reservations/${id}/timeline`)
}

export function actOnReservation(id, action) {
  return apiRequest(`/api/reservations/${id}/${action}`, { method: 'POST' })
}
