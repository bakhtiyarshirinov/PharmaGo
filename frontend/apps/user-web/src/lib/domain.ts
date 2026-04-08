import type { NotificationHistoryItem, Reservation } from '@pharmago/types'

export function getReservationStatusLabel(status: number) {
  switch (status) {
    case 1:
      return 'Pending'
    case 2:
      return 'Confirmed'
    case 3:
      return 'Ready for pickup'
    case 4:
      return 'Completed'
    case 5:
      return 'Cancelled'
    case 6:
      return 'Expired'
    default:
      return 'Unknown'
  }
}

export function getReservationStatusTone(status: number): 'warning' | 'info' | 'success' | 'danger' | 'neutral' {
  switch (status) {
    case 1:
      return 'warning'
    case 2:
      return 'info'
    case 3:
      return 'success'
    case 4:
      return 'success'
    case 5:
    case 6:
      return 'danger'
    default:
      return 'neutral'
  }
}

export function canCancelReservation(reservation: Reservation) {
  return reservation.status === 1 || reservation.status === 2 || reservation.status === 3
}

export function getNotificationEventLabel(eventType: NotificationHistoryItem['eventType']) {
  switch (eventType) {
    case 1:
      return 'Reservation confirmed'
    case 2:
      return 'Ready for pickup'
    case 3:
      return 'Reservation cancelled'
    case 4:
      return 'Reservation expired'
    case 5:
      return 'Reservation expiring soon'
    default:
      return 'Reservation update'
  }
}

export function getNotificationStatusLabel(status: NotificationHistoryItem['status']) {
  switch (status) {
    case 1:
      return 'Delivered'
    case 2:
      return 'Skipped'
    case 3:
      return 'Failed'
    default:
      return 'Unknown'
  }
}

export function getNotificationStatusTone(status: NotificationHistoryItem['status']): 'success' | 'warning' | 'danger' | 'neutral' {
  switch (status) {
    case 1:
      return 'success'
    case 2:
      return 'warning'
    case 3:
      return 'danger'
    default:
      return 'neutral'
  }
}

interface WeeklyHoursEntry {
  day: string
  open: string
  close: string
}

interface OpeningHoursDocument {
  timeZone?: string
  weekly?: WeeklyHoursEntry[]
}

export function parseOpeningHours(input?: string | null) {
  if (!input) {
    return []
  }

  try {
    const parsed = JSON.parse(input) as OpeningHoursDocument
    return parsed.weekly ?? []
  } catch {
    return []
  }
}
