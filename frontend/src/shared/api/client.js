import { useSessionStore } from '../../features/auth/model/useSessionStore'

const API_BASE = (import.meta.env.VITE_API_BASE_URL || '').replace(/\/$/, '')

function buildUrl(path) {
  return `${API_BASE}${path}`
}

export async function apiRequest(path, options = {}) {
  const token = useSessionStore.getState().session?.accessToken
  const headers = {
    ...(options.body ? { 'Content-Type': 'application/json' } : {}),
    ...(options.headers || {}),
  }

  if (token) {
    headers.Authorization = `Bearer ${token}`
  }

  const response = await fetch(buildUrl(path), {
    ...options,
    headers,
  })

  const text = await response.text()
  let data = null

  if (text) {
    try {
      data = JSON.parse(text)
    } catch {
      data = text
    }
  }

  if (!response.ok) {
    const message =
      typeof data === 'string'
        ? data
        : data?.title || data?.detail || data?.message || `${response.status} ${response.statusText}`
    throw new Error(message)
  }

  return data
}

export async function healthRequest() {
  const response = await fetch(buildUrl('/health'))
  if (!response.ok) {
    throw new Error('Health check failed')
  }
  return 'online'
}
