'use client'

import { createContext, useContext, useEffect, useMemo, useRef } from 'react'
import type { AuthSession } from '@pharmago/types'
import { getAuthCookieNames, type AuthPortal } from '@pharmago/config'
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

  if (!response.ok) {
    throw await response.json()
  }

  return response.json() as Promise<T>
}

function getSessionFromCookie(portal: AuthPortal): AuthSession | null {
  if (typeof document === 'undefined') {
    return null
  }

  const cookieNames = getAuthCookieNames(portal)
  const rawCookie = document.cookie
    .split('; ')
    .find((entry) => entry.startsWith(`${cookieNames.sessionMeta}=`))
    ?.slice(cookieNames.sessionMeta.length + 1)

  if (!rawCookie) {
    return null
  }

  try {
    return JSON.parse(decodeURIComponent(rawCookie)) as AuthSession
  } catch {
    return null
  }
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
      await postJson<void>('/api/auth/logout')
      setSession(null)
    },
    async refreshSession() {
      if (refreshInFlight.current) {
        return refreshInFlight.current
      }

      const refreshPromise = (async () => {
        const cookieSession = getSessionFromCookie(portal)

        if (cookieSession && !useAuthStore.getState().session) {
          setSession(cookieSession)
        }

        const response = await fetch('/api/auth/session', {
          credentials: 'include',
          cache: 'no-store',
        })

        if (response.status === 401) {
          setSession(null)
          return null
        }

        if (!response.ok) {
          throw await response.json()
        }

        const nextSession = await response.json() as AuthSession
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
    const cookieSession = getSessionFromCookie(portal)

    if (cookieSession && !session) {
      setSession(cookieSession)
    }

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
