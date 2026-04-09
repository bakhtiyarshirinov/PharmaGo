'use client'

import { useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { browserApi } from '../../lib/api'
import { getApiErrorMessage } from '../../lib/errors'

function useAdminMutation<TInput, TOutput>(
  successMessage: string,
  mutationFn: (input: TInput) => Promise<TOutput>,
) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn,
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['admin', 'users'] }),
        queryClient.invalidateQueries({ queryKey: ['admin', 'overview'] }),
        queryClient.invalidateQueries({ queryKey: ['admin', 'audit'] }),
      ])
      toast.success(successMessage)
    },
    onError: (error) => {
      toast.error(getApiErrorMessage(error, 'Не удалось обновить пользователя.'))
    },
  })
}

export function useCreateUser() {
  return useAdminMutation('Пользователь создан.', (input: Parameters<typeof browserApi.admin.createUser>[0]) =>
    browserApi.admin.createUser(input))
}

export function useUpdateUser(userId: string) {
  return useAdminMutation('Пользователь обновлен.', (input: Parameters<typeof browserApi.admin.updateUser>[1]) =>
    browserApi.admin.updateUser(userId, input))
}

export function useDeactivateUser() {
  return useAdminMutation('Пользователь деактивирован.', (userId: string) =>
    browserApi.admin.deactivateUser(userId))
}

export function useRestoreUser() {
  return useAdminMutation('Пользователь восстановлен.', (userId: string) =>
    browserApi.admin.restoreUser(userId))
}
