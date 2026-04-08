'use client'

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import {
  type AdjustStockQuantityInput,
  type CreateStockItemInput,
  type ReceiveStockInput,
  type UpdateStockItemInput,
  type WriteOffStockInput,
} from '@pharmago/api-client'
import type { StockItem } from '@pharmago/types'
import { browserApi } from '../../lib/api'
import { getApiErrorMessage } from '../../lib/errors'
import { queryKeys } from '../../lib/query-keys'

export function useInventoryStock(pharmacyId?: string | null, lowStockOnly = false) {
  return useQuery({
    queryKey: queryKeys.inventory.stock(pharmacyId, lowStockOnly),
    queryFn: () => browserApi.stocks.byPharmacy(pharmacyId!, { lowStockOnly }),
    enabled: Boolean(pharmacyId),
  })
}

export function useLowStockAlerts(pharmacyId?: string | null) {
  return useQuery({
    queryKey: queryKeys.inventory.lowStock(pharmacyId),
    queryFn: () => browserApi.stocks.lowStock(pharmacyId ?? undefined),
    enabled: Boolean(pharmacyId),
  })
}

export function useOutOfStockAlerts(pharmacyId?: string | null) {
  return useQuery({
    queryKey: queryKeys.inventory.outOfStock(pharmacyId),
    queryFn: () => browserApi.stocks.outOfStock(pharmacyId ?? undefined),
    enabled: Boolean(pharmacyId),
  })
}

export function useExpiringAlerts(pharmacyId?: string | null, days = 30) {
  return useQuery({
    queryKey: queryKeys.inventory.expiring(pharmacyId, days),
    queryFn: () => browserApi.stocks.expiring(days, pharmacyId ?? undefined),
    enabled: Boolean(pharmacyId),
  })
}

export function useRestockSuggestions(pharmacyId?: string | null) {
  return useQuery({
    queryKey: queryKeys.inventory.restock(pharmacyId),
    queryFn: () => browserApi.stocks.restockSuggestions(pharmacyId ?? undefined),
    enabled: Boolean(pharmacyId),
  })
}

export function useMedicineSuggestions(query: string) {
  return useQuery({
    queryKey: queryKeys.inventory.medicineSuggestions(query),
    queryFn: () => browserApi.medicines.suggestions(query, 8),
    enabled: query.trim().length >= 2,
  })
}

function useInventoryMutation<TInput>(
  successMessage: string,
  mutationFn: (input: TInput) => Promise<StockItem>,
) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn,
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['inventory'] }),
        queryClient.invalidateQueries({ queryKey: ['dashboard'] }),
      ])

      toast.success(successMessage)
    },
    onError: (error) => {
      toast.error(getApiErrorMessage(error, 'Не удалось обновить складские данные.'))
    },
  })
}

export function useCreateStockItem() {
  return useInventoryMutation<CreateStockItemInput>('Партия добавлена в остатки.', (input) => browserApi.stocks.create(input))
}

export function useUpdateStockItem(stockItemId: string) {
  return useInventoryMutation<UpdateStockItemInput>('Партия обновлена.', (input) =>
    browserApi.stocks.update(stockItemId, input),
  )
}

export function useAdjustStockItem(stockItemId: string) {
  return useInventoryMutation<AdjustStockQuantityInput>('Корректировка применена.', (input) =>
    browserApi.stocks.adjust(stockItemId, input),
  )
}

export function useReceiveStockItem(stockItemId: string) {
  return useInventoryMutation<ReceiveStockInput>('Поступление проведено.', (input) =>
    browserApi.stocks.receive(stockItemId, input),
  )
}

export function useWriteOffStockItem(stockItemId: string) {
  return useInventoryMutation<WriteOffStockInput>('Списание проведено.', (input) =>
    browserApi.stocks.writeoff(stockItemId, input),
  )
}
