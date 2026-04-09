import type { AuthSession, Role } from '@pharmago/types'
import { create } from 'zustand'

interface AuthStore {
  session: AuthSession | null
  isHydrating: boolean
  postLoginRedirect: string | null
  setSession: (session: AuthSession | null) => void
  setHydrating: (value: boolean) => void
  setPostLoginRedirect: (path: string | null) => void
}

export const useAuthStore = create<AuthStore>((set) => ({
  session: null,
  isHydrating: true,
  postLoginRedirect: null,
  setSession: (session) => set({ session }),
  setHydrating: (isHydrating) => set({ isHydrating }),
  setPostLoginRedirect: (postLoginRedirect) => set({ postLoginRedirect }),
}))

export function mapBackendRole(role: number): Role {
  if (role === 2) {
    return 'pharmacist'
  }

  if (role === 3) {
    return 'admin'
  }

  return 'consumer'
}
