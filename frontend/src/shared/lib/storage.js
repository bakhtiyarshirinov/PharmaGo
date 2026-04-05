const STORAGE_KEY = 'pharmago.frontend.session'

export function readStoredSession() {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    return raw ? JSON.parse(raw) : null
  } catch {
    return null
  }
}

export function writeStoredSession(session) {
  if (!session) {
    localStorage.removeItem(STORAGE_KEY)
    return
  }

  localStorage.setItem(STORAGE_KEY, JSON.stringify(session))
}
