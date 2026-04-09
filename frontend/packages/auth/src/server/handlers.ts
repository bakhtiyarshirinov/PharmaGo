import { NextResponse } from 'next/server'
import type { AuthSession } from '@pharmago/types'
import { backendAuthRequest } from './backend-auth'
import { canAccessPortal, type Portal } from './portal-access'
import { clearAuthCookies, readRefreshToken, readSessionMeta, writeAuthCookies } from './session'

interface AuthHandlerOptions {
  portal?: Portal
}

function toSession(auth: {
  accessToken: string
  expiresAtUtc: string
  user: AuthSession['user']
}): AuthSession {
  return {
    accessToken: auth.accessToken,
    expiresAtUtc: auth.expiresAtUtc,
    user: auth.user,
  }
}

function getPortalErrorDetail(portal: Portal) {
  switch (portal) {
    case 'pharmacist':
      return 'Этот аккаунт не имеет доступа к рабочему месту фармацевта.'
    case 'admin':
      return 'Этот аккаунт не имеет доступа к панели модератора.'
    default:
      return 'Этот аккаунт не имеет доступа к пользовательскому порталу.'
  }
}

async function guardPortalAccess(session: AuthSession, portal?: Portal) {
  if (!portal || canAccessPortal(session, portal)) {
    return null
  }

  await clearAuthCookies(portal)

  return NextResponse.json(
    {
      title: 'Forbidden',
      status: 403,
      detail: getPortalErrorDetail(portal),
    },
    { status: 403 },
  )
}

export async function loginHandler(request: Request, options: AuthHandlerOptions = {}) {
  const body = await request.json()
  const auth = await backendAuthRequest('/api/v1/auth/login', body)
  const session = toSession(auth)
  const portalError = await guardPortalAccess(session, options.portal)

  if (portalError) {
    return portalError
  }

  await writeAuthCookies(auth, options.portal ?? 'user')

  return NextResponse.json(session)
}

export async function logoutHandler(options: AuthHandlerOptions = {}) {
  const portal = options.portal ?? 'user'
  const refreshToken = await readRefreshToken(portal)

  if (refreshToken) {
    try {
      await backendAuthRequest('/api/v1/auth/logout', { refreshToken })
    } catch {
      // Ignore logout backend failure and clear local session anyway.
    }
  }

  await clearAuthCookies(portal)
  return NextResponse.json({ ok: true })
}

export async function sessionHandler(options: AuthHandlerOptions = {}) {
  const portal = options.portal ?? 'user'
  const current = await readSessionMeta(portal)

  if (current && new Date(current.expiresAtUtc).getTime() > Date.now() + 30_000) {
    const portalError = await guardPortalAccess(current, options.portal)

    if (portalError) {
      return portalError
    }

    return NextResponse.json(current)
  }

  const refreshToken = await readRefreshToken(portal)
  if (!refreshToken) {
    return NextResponse.json({ title: 'Unauthorized', status: 401 }, { status: 401 })
  }

  try {
    const nextAuth = await backendAuthRequest('/api/v1/auth/refresh', { refreshToken })
    const nextSession = toSession(nextAuth)
    const portalError = await guardPortalAccess(nextSession, options.portal)

    if (portalError) {
      return portalError
    }

    await writeAuthCookies(nextAuth, portal)
    return NextResponse.json(nextSession)
  } catch {
    await clearAuthCookies(portal)
    return NextResponse.json({ title: 'Unauthorized', status: 401 }, { status: 401 })
  }
}
