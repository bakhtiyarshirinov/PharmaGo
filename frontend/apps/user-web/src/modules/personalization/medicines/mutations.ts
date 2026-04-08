'use client'

import { useMutation, useQueryClient, type QueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import type { ConsumerMedicineFeedItem } from '@pharmago/types'
import { getApiErrorMessage } from '../../../lib/errors'
import { queryKeys } from '../../../lib/query-keys'
import { meMedicinesApi } from './api'

function updateMedicineFavoriteState(
  queryClient: QueryClient,
  medicineId: string,
  nextIsFavorite: boolean,
) {
  const now = new Date().toISOString()

  queryClient.setQueriesData<ConsumerMedicineFeedItem[]>(
    { queryKey: ['personalization', 'medicines'] },
    (current) =>
      current?.map((item) =>
        item.medicineId === medicineId
          ? {
              ...item,
              isFavorite: nextIsFavorite,
              favoritedAtUtc: nextIsFavorite ? now : null,
            }
          : item,
      ),
  )
}

export function useToggleMedicineFavorite() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ medicineId, isFavorite }: { medicineId: string; isFavorite: boolean }) => {
      if (isFavorite) {
        await meMedicinesApi.removeFavorite(medicineId)
        return { medicineId, isFavorite: false }
      }

      await meMedicinesApi.addFavorite(medicineId)
      return { medicineId, isFavorite: true }
    },
    onMutate: async ({ medicineId, isFavorite }) => {
      await queryClient.cancelQueries({ queryKey: queryKeys.personalization.medicines.all() })
      updateMedicineFavoriteState(queryClient, medicineId, !isFavorite)
      return { medicineId, previousIsFavorite: isFavorite }
    },
    onSuccess: (result) => {
      toast.success(result.isFavorite ? 'Medicine added to favorites' : 'Medicine removed from favorites')
      queryClient.invalidateQueries({ queryKey: queryKeys.personalization.medicines.all() })
    },
    onError: (error, _variables, context) => {
      if (context) {
        updateMedicineFavoriteState(queryClient, context.medicineId, context.previousIsFavorite)
      }

      toast.error(getApiErrorMessage(error, 'Unable to update medicine favorite'))
    },
  })
}
