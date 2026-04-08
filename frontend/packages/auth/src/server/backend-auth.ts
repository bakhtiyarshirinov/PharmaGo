import { backendUrl } from '@pharmago/config'
import type { AuthResponse } from '@pharmago/types'

interface BackendUserProfile {
  id?: string
  Id: string
  firstName?: string
  FirstName: string
  lastName?: string
  LastName: string
  phoneNumber?: string
  PhoneNumber: string
  email?: string | null
  Email?: string | null
  telegramUsername?: string | null
  TelegramUsername?: string | null
  telegramChatId?: string | null
  TelegramChatId?: string | null
  role?: number
  Role: number
  pharmacyId?: string | null
  PharmacyId?: string | null
}

interface BackendAuthResponse {
  accessToken?: string
  AccessToken: string
  expiresAtUtc?: string
  ExpiresAtUtc: string
  refreshToken?: string
  RefreshToken: string
  refreshTokenExpiresAtUtc?: string
  RefreshTokenExpiresAtUtc: string
  user?: BackendUserProfile
  User: BackendUserProfile
}

function normalizeAuthResponse(payload: BackendAuthResponse): AuthResponse {
  const user = payload.user ?? payload.User

  return {
    accessToken: payload.accessToken ?? payload.AccessToken,
    expiresAtUtc: payload.expiresAtUtc ?? payload.ExpiresAtUtc,
    refreshToken: payload.refreshToken ?? payload.RefreshToken,
    refreshTokenExpiresAtUtc: payload.refreshTokenExpiresAtUtc ?? payload.RefreshTokenExpiresAtUtc,
    user: {
      id: user.id ?? user.Id,
      firstName: user.firstName ?? user.FirstName,
      lastName: user.lastName ?? user.LastName,
      phoneNumber: user.phoneNumber ?? user.PhoneNumber,
      email: user.email ?? user.Email,
      telegramUsername: user.telegramUsername ?? user.TelegramUsername,
      telegramChatId: user.telegramChatId ?? user.TelegramChatId,
      role: user.role ?? user.Role,
      pharmacyId: user.pharmacyId ?? user.PharmacyId,
    },
  }
}

export async function backendAuthRequest(path: string, body?: unknown) {
  const response = await fetch(`${backendUrl}${path}`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: body ? JSON.stringify(body) : undefined,
    cache: 'no-store',
  })

  const payload = await response.json().catch(() => null)

  if (!response.ok) {
    throw payload ?? { title: 'Authentication failed', status: response.status }
  }

  return normalizeAuthResponse(payload as BackendAuthResponse)
}
