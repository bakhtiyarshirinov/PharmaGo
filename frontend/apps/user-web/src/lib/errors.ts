import { ApiError } from '@pharmago/api-client'

export function getApiErrorMessage(error: unknown, fallback = 'Something went wrong') {
  return error instanceof ApiError ? error.details?.detail || error.message : fallback
}
