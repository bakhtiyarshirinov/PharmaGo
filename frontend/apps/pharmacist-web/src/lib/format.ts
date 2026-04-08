import type {
  ExpiringStockAlert,
  NotificationHistoryItem,
  OutOfStockAlert,
  Reservation,
  StockItem,
} from '@pharmago/types'

const dateTimeFormatter = new Intl.DateTimeFormat('ru-RU', {
  dateStyle: 'medium',
  timeStyle: 'short',
})

const dateFormatter = new Intl.DateTimeFormat('ru-RU', {
  dateStyle: 'medium',
})

const moneyFormatter = new Intl.NumberFormat('ru-RU', {
  style: 'currency',
  currency: 'AZN',
  maximumFractionDigits: 2,
})

export function formatDateTime(value?: string | null) {
  if (!value) {
    return 'Нет данных'
  }

  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? value : dateTimeFormatter.format(date)
}

export function formatDate(value?: string | null) {
  if (!value) {
    return 'Нет данных'
  }

  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? value : dateFormatter.format(date)
}

export function formatMoney(value?: number | null) {
  return moneyFormatter.format(value ?? 0)
}

export function formatNumber(value?: number | null) {
  return new Intl.NumberFormat('ru-RU').format(value ?? 0)
}

export function getReservationStatusMeta(status: number) {
  switch (status) {
    case 1:
      return { label: 'Ожидает подтверждения', tone: 'warning' as const }
    case 2:
      return { label: 'Подтвержден', tone: 'info' as const }
    case 3:
      return { label: 'Готов к выдаче', tone: 'success' as const }
    case 4:
      return { label: 'Выдан', tone: 'success' as const }
    case 5:
      return { label: 'Отменен', tone: 'danger' as const }
    case 6:
      return { label: 'Истек', tone: 'danger' as const }
    default:
      return { label: 'Неизвестно', tone: 'neutral' as const }
  }
}

export function getNotificationEventLabel(eventType: number) {
  switch (eventType) {
    case 1:
      return 'Подтверждение резерва'
    case 2:
      return 'Готов к выдаче'
    case 3:
      return 'Отмена резерва'
    case 4:
      return 'Резерв истек'
    case 5:
      return 'Скоро истечет'
    case 6:
      return 'Резерв выдан'
    default:
      return 'Служебное уведомление'
  }
}

export function getNotificationStatusMeta(status: number) {
  switch (status) {
    case 1:
      return { label: 'Отправлено', tone: 'success' as const }
    case 2:
      return { label: 'Пропущено', tone: 'warning' as const }
    case 3:
      return { label: 'Ошибка', tone: 'danger' as const }
    default:
      return { label: 'Неизвестно', tone: 'neutral' as const }
  }
}

export function getNotificationChannelLabel(channel: number) {
  return channel === 2 ? 'Telegram' : 'In-app'
}

export function getStockHealthTone(item: StockItem | OutOfStockAlert | ExpiringStockAlert) {
  if ('daysUntilExpiration' in item && item.daysUntilExpiration <= 7) {
    return 'danger' as const
  }

  if ('totalAvailableQuantity' in item && item.totalAvailableQuantity <= 0) {
    return 'danger' as const
  }

  if ('isLowStock' in item && item.isLowStock) {
    return 'warning' as const
  }

  return 'neutral' as const
}

export function getReservationCountdown(reservation: Reservation) {
  const diffMs = new Date(reservation.reservedUntilUtc).getTime() - Date.now()
  const diffMinutes = Math.round(diffMs / 60000)

  if (diffMinutes <= 0) {
    return 'Срок уже истек'
  }

  if (diffMinutes < 60) {
    return `${diffMinutes} мин до истечения`
  }

  const hours = Math.floor(diffMinutes / 60)
  const minutes = diffMinutes % 60
  return `${hours} ч ${minutes} мин до истечения`
}

export function canCompleteReservation(reservation: Reservation) {
  if (reservation.status !== 3) {
    return false
  }

  if (!reservation.pickupAvailableFromUtc) {
    return true
  }

  return new Date(reservation.pickupAvailableFromUtc).getTime() <= Date.now()
}

export function getCompleteGuardMessage(reservation: Reservation) {
  if (canCompleteReservation(reservation)) {
    return null
  }

  return `Выдача станет доступна ${formatDateTime(reservation.pickupAvailableFromUtc)}.`
}

export function getNotificationPreviewTone(notification: NotificationHistoryItem) {
  if (!notification.isRead) {
    return 'info' as const
  }

  return getNotificationStatusMeta(notification.status).tone
}
