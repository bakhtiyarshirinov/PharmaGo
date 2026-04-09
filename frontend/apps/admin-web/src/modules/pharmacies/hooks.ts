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
        queryClient.invalidateQueries({ queryKey: ['admin', 'pharmacies'] }),
        queryClient.invalidateQueries({ queryKey: ['admin', 'overview'] }),
        queryClient.invalidateQueries({ queryKey: ['admin', 'audit'] }),
      ])
      toast.success(successMessage)
    },
    onError: (error) => {
      toast.error(getApiErrorMessage(error, 'Не удалось обновить аптеку.'))
    },
  })
}

export function useCreatePharmacy() {
  return useAdminMutation('Аптека создана.', (input: Parameters<typeof browserApi.admin.createPharmacy>[0]) =>
    browserApi.admin.createPharmacy(input))
}

export function useUpdatePharmacy(pharmacyId: string) {
  return useAdminMutation('Аптека обновлена.', (input: Parameters<typeof browserApi.admin.updatePharmacy>[1]) =>
    browserApi.admin.updatePharmacy(pharmacyId, input))
}

export function useDeactivatePharmacy() {
  return useAdminMutation('Аптека деактивирована.', (pharmacyId: string) =>
    browserApi.admin.deactivatePharmacy(pharmacyId))
}

export function useRestorePharmacy() {
  return useAdminMutation('Аптека восстановлена.', (pharmacyId: string) =>
    browserApi.admin.restorePharmacy(pharmacyId))
}
