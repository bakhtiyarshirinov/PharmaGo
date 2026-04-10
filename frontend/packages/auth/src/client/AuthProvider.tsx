'use client'

import { createContext, useContext, useEffect, useMemo, useRef } from 'react'
import type { AuthSession } from '@pharmago/types'
import type { AuthPortal } from '@pharmago/config'
import { useAuthStore } from './store'

interface AuthContextValue {
  session: AuthSession | null
  isHydrating: boolean
  login: (input: { phoneNumber: string; password: string }) => Promise<AuthSession>
  logout: () => Promise<void>
  refreshSession: () => Promise<AuthSession | null>
}

const AuthContext = createContext<AuthContextValue | null>(null)

async function postJson<T>(url: string, body?: unknown): Promise<T> {
  const response = await fetch(url, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    credentials: 'include',
    body: body ? JSON.stringify(body) : undefined,
  })

  const payload = await readJsonSafely(response)

  if (!response.ok) {
    throw createResponseError(payload, response.statusText)
  }

  return payload as T
}

export function AuthProvider({
  children,
  portal,
}: {
  children: React.ReactNode
  portal: AuthPortal
}) {
  const session = useAuthStore((state) => state.session)
  const isHydrating = useAuthStore((state) => state.isHydrating)
  const setSession = useAuthStore((state) => state.setSession)
  const setHydrating = useAuthStore((state) => state.setHydrating)
  const refreshInFlight = useRef<Promise<AuthSession | null> | null>(null)

  const api = useMemo<AuthContextValue>(() => ({
    session,
    isHydrating,
    async login(input) {
      const nextSession = await postJson<AuthSession>('/api/auth/login', input)
      setSession(nextSession)
      return nextSession
    },
    async logout() {
      try {
        await postJson<void>('/api/auth/logout')
      } finally {
        setSession(null)
      }
    },
    async refreshSession() {
      if (refreshInFlight.current) {
        return refreshInFlight.current
      }

      const refreshPromise = (async () => {
        const response = await fetch('/api/auth/session', {
          credentials: 'include',
          cache: 'no-store',
        })

        if (response.status === 401) {
          setSession(null)
          return null
        }

        const payload = await readJsonSafely(response)

        if (!response.ok) {
          throw createResponseError(payload, response.statusText)
        }

        const nextSession = payload as AuthSession
        setSession(nextSession)
        return nextSession
      })()

      refreshInFlight.current = refreshPromise

      try {
        return await refreshPromise
      } finally {
        refreshInFlight.current = null
      }
    },
  }), [isHydrating, portal, session, setSession])

  useEffect(() => {
    api.refreshSession()
      .catch(() => {
        setSession(null)
      })
      .finally(() => {
        setHydrating(false)
      })
  }, [api, portal, session, setHydrating, setSession])

  useEffect(() => {
    function refreshOnForeground() {
      if (document.visibilityState === 'visible') {
        void api.refreshSession().catch(() => {
          setSession(null)
        })
      }
    }

    window.addEventListener('focus', refreshOnForeground)
    document.addEventListener('visibilitychange', refreshOnForeground)

    return () => {
      window.removeEventListener('focus', refreshOnForeground)
      document.removeEventListener('visibilitychange', refreshOnForeground)
    }
  }, [api, setSession])

  useEffect(() => {
    if (!session?.expiresAtUtc) {
      return
    }

    const expiresAtMs = new Date(session.expiresAtUtc).getTime()
    const delayMs = Math.max(15_000, expiresAtMs - Date.now() - 60_000)
    const timer = window.setTimeout(() => {
      void api.refreshSession().catch(() => {
        setSession(null)
      })
    }, delayMs)

    return () => {
      window.clearTimeout(timer)
    }
  }, [api, session?.expiresAtUtc, setSession])

  return <AuthContext.Provider value={api}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const context = useContext(AuthContext)

  if (!context) {
    throw new Error('useAuth must be used within AuthProvider')
  }

  return context
}

async function readJsonSafely(response: Response): Promise<unknown> {
  const raw = await response.text()

  if (!raw) {
    return null
  }

  try {
    return JSON.parse(raw)
  } catch {
    return { detail: raw }
  }
}

function createResponseError(payload: unknown, fallbackMessage: string) {
  if (payload instanceof Error) {
    return payload
  }

  if (payload && typeof payload === 'object') {
    return payload
  }

  return new Error(fallbackMessage || 'Request failed')
}
