'use client'

import { useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { browserApi } from '../../lib/api'
import { getApiErrorMessage } from '../../lib/errors'

function useMasterDataMutation<TInput, TOutput>(
  successMessage: string,
  mutationFn: (input: TInput) => Promise<TOutput>,
) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn,
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['admin', 'master-data'] }),
        queryClient.invalidateQueries({ queryKey: ['admin', 'overview'] }),
        queryClient.invalidateQueries({ queryKey: ['admin', 'audit'] }),
      ])
      toast.success(successMessage)
    },
    onError: (error) => {
      toast.error(getApiErrorMessage(error, 'Не удалось обновить master-data.'))
    },
  })
}

export function useCreateMedicine() {
  return useMasterDataMutation('Лекарство создано.', (input: Parameters<typeof browserApi.admin.createMedicine>[0]) =>
    browserApi.admin.createMedicine(input))
}

export function useUpdateMedicine(medicineId: string) {
  return useMasterDataMutation('Лекарство обновлено.', (input: Parameters<typeof browserApi.admin.updateMedicine>[1]) =>
    browserApi.admin.updateMedicine(medicineId, input))
}

export function useCreateMedicineCategory() {
  return useMasterDataMutation(
    'Категория создана.',
    (input: Parameters<typeof browserApi.admin.createMedicineCategory>[0]) =>
      browserApi.admin.createMedicineCategory(input),
  )
}

export function useUpdateMedicineCategory(categoryId: string) {
  return useMasterDataMutation(
    'Категория обновлена.',
    (input: Parameters<typeof browserApi.admin.updateMedicineCategory>[1]) =>
      browserApi.admin.updateMedicineCategory(categoryId, input),
  )
}
