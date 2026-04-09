'use client'

import { createApiClient } from '@pharmago/api-client'
import { backendUrl } from '@pharmago/config'
import { useAuthStore } from '@pharmago/auth/client'

export const browserApi = createApiClient({
  baseUrl: backendUrl,
  getAccessToken: () => useAuthStore.getState().session?.accessToken ?? null,
  onUnauthorized: () => {
    useAuthStore.getState().setSession(null)
  },
})
