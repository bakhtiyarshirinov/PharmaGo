import type { AuthResponse, AuthSession } from '@pharmago/types'
import { cookies } from 'next/headers'
import { getAuthCookieNames } from '@pharmago/config'
import type { Portal } from './portal-access'

export async function readSessionMeta(portal: Portal): Promise<AuthSession | null> {
  const cookieNames = getAuthCookieNames(portal)
  const raw = (await cookies()).get(cookieNames.sessionMeta)?.value
  if (!raw) {
    return null
  }

  try {
    return JSON.parse(raw) as AuthSession
  } catch {
    return null
  }
}

export async function readRefreshToken(portal: Portal): Promise<string | null> {
  const cookieNames = getAuthCookieNames(portal)
  return (await cookies()).get(cookieNames.refreshToken)?.value ?? null
}

export async function writeAuthCookies(auth: AuthResponse, portal: Portal) {
  const cookieStore = await cookies()
  const cookieNames = getAuthCookieNames(portal)
  const accessExpiresAt = new Date(auth.expiresAtUtc)
  const refreshExpiresAt = new Date(auth.refreshTokenExpiresAtUtc)

  cookieStore.set(cookieNames.refreshToken, auth.refreshToken, {
    httpOnly: true,
    sameSite: 'lax',
    secure: false,
    path: '/',
    expires: refreshExpiresAt,
  })

  cookieStore.set(cookieNames.sessionMeta, JSON.stringify({
    accessToken: auth.accessToken,
    expiresAtUtc: auth.expiresAtUtc,
    user: auth.user,
  } satisfies AuthSession), {
    httpOnly: true,
    sameSite: 'lax',
    secure: false,
    path: '/',
    expires: accessExpiresAt,
  })
}

export async function clearAuthCookies(portal: Portal) {
  const cookieStore = await cookies()
  const cookieNames = getAuthCookieNames(portal)
  cookieStore.delete(cookieNames.refreshToken)
  cookieStore.delete(cookieNames.sessionMeta)
}
