import dayjs from 'dayjs'
import relativeTime from 'dayjs/plugin/relativeTime'

dayjs.extend(relativeTime)

export function formatMoney(value?: number | null) {
  if (value === undefined || value === null) {
    return 'Price unavailable'
  }

  return new Intl.NumberFormat('en-AZ', {
    style: 'currency',
    currency: 'AZN',
    maximumFractionDigits: 2,
  }).format(value)
}

export function formatDateTime(value?: string | null) {
  if (!value) {
    return 'Not available'
  }

  const date = new Date(value)
  if (Number.isNaN(date.getTime())) {
    return value
  }

  return new Intl.DateTimeFormat('en-GB', {
    dateStyle: 'medium',
    timeStyle: 'short',
    timeZone: 'Asia/Baku',
  }).format(date)
}

export function formatRelativeTime(value?: string | null) {
  if (!value) {
    return 'No timestamp'
  }

  const date = dayjs(value)
  if (!date.isValid()) {
    return value
  }

  return date.fromNow()
}

export function formatDistance(distanceKm?: number | null) {
  if (distanceKm === undefined || distanceKm === null) {
    return 'Distance unavailable'
  }

  return `${distanceKm.toFixed(1)} km away`
}
