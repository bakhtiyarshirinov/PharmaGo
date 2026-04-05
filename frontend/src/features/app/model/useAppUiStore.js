import { create } from 'zustand'

export const useAppUiStore = create((set) => ({
  error: '',
  toast: '',
  setError: (error) => set({ error }),
  setToast: (toast) => set({ toast }),
}))
