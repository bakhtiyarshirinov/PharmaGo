import { create } from 'zustand'
import { readStoredSession, writeStoredSession } from '../../../shared/lib/storage'

export const useSessionStore = create((set) => ({
  session: readStoredSession(),
  loginOpen: false,
  setSession: (session) => {
    writeStoredSession(session)
    set({ session })
  },
  clearSession: () => {
    writeStoredSession(null)
    set({ session: null })
  },
  setLoginOpen: (loginOpen) => set({ loginOpen }),
}))
