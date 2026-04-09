export function formatDateTime(value?: string | null) {
  if (!value) {
    return 'Нет данных'
  }

  const date = new Date(value)

  if (Number.isNaN(date.getTime())) {
    return value
  }

  return new Intl.DateTimeFormat('ru-RU', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(date)
}

export function formatNumber(value?: number | null) {
  return new Intl.NumberFormat('ru-RU').format(value ?? 0)
}

export function formatMoney(value?: number | null) {
  return new Intl.NumberFormat('ru-RU', {
    style: 'currency',
    currency: 'AZN',
    maximumFractionDigits: 2,
  }).format(value ?? 0)
}

export function getUserRoleLabel(role: number) {
  switch (role) {
    case 2:
      return 'Фармацевт'
    case 3:
      return 'Модератор'
    default:
      return 'Пользователь'
  }
}

export function getUserRoleTone(role: number) {
  switch (role) {
    case 2:
      return 'warning' as const
    case 3:
      return 'info' as const
    default:
      return 'neutral' as const
  }
}
