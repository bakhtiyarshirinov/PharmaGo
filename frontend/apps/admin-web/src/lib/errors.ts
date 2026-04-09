import { ApiError } from '@pharmago/api-client'

export function getApiErrorMessage(error: unknown, fallback = 'Не удалось выполнить операцию.') {
  return error instanceof ApiError ? error.details?.detail || error.message : fallback
}
