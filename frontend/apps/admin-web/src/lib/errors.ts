import { ApiError } from '@pharmago/api-client'

export function getApiErrorMessage(error: unknown, fallback = 'Не удалось выполнить операцию.') {
  if (error instanceof ApiError) {
    return error.details?.detail || error.message || fallback
  }

  if (error instanceof Error) {
    return error.message || fallback
  }

  if (isProblemLike(error)) {
    return error.detail || error.title || fallback
  }

  return fallback
}

function isProblemLike(error: unknown): error is { detail?: string; title?: string } {
  return Boolean(error && typeof error === 'object' && ('detail' in error || 'title' in error))
}
