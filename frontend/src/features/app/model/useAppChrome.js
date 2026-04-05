import { useMutation, useQuery } from '@tanstack/react-query'
import { healthRequest } from '../../../shared/api/client'
import { logout } from '../../../shared/api/auth'
import { useSessionStore } from '../../auth/model/useSessionStore'
import { useAppUiStore } from './useAppUiStore'

export function useAppChrome() {
  const session = useSessionStore((state) => state.session)
  const loginOpen = useSessionStore((state) => state.loginOpen)
  const setLoginOpen = useSessionStore((state) => state.setLoginOpen)
  const clearSession = useSessionStore((state) => state.clearSession)
  const error = useAppUiStore((state) => state.error)
  const toast = useAppUiStore((state) => state.toast)
  const setError = useAppUiStore((state) => state.setError)
  const setToast = useAppUiStore((state) => state.setToast)

  const healthQuery = useQuery({
    queryKey: ['health'],
    queryFn: healthRequest,
    retry: 0,
  })

  const logoutMutation = useMutation({
    mutationFn: async () => {
      if (session?.refreshToken) {
        await logout(session.refreshToken)
      }
    },
    onSuccess: () => {
      clearSession()
      setToast('Logged out')
    },
    onError: (mutationError) => {
      setError(mutationError.message)
      clearSession()
    },
  })

  return {
    session,
    loginOpen,
    setLoginOpen,
    error,
    toast,
    setError,
    setToast,
    health: healthQuery.isError ? 'offline' : healthQuery.isPending ? 'checking' : 'online',
    handleLogout: logoutMutation.mutateAsync,
  }
}
