import dayjs from 'dayjs'

export function normalizeItems(payload) {
  if (Array.isArray(payload)) {
    return payload
  }

  if (Array.isArray(payload?.items)) {
    return payload.items
  }

  if (Array.isArray(payload?.availabilities)) {
    return payload.availabilities
  }

  return []
}

export function formatDate(value) {
  return value ? dayjs(value).format('DD MMM YYYY, HH:mm') : 'Not available'
}

export function formatMoney(value) {
  if (value === null || value === undefined) {
    return 'N/A'
  }

  return `$${Number(value).toFixed(2)}`
}

export function getStatusTone(status) {
  switch (status) {
    case 'online':
      return 'success'
    case 'degraded':
      return 'warning'
    case 'offline':
      return 'error'
    default:
      return 'default'
  }
}
