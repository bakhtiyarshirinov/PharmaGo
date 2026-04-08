'use client'

import { useMutation, useQueryClient, type QueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import type { ConsumerPharmacyFeedItem } from '@pharmago/types'
import { getApiErrorMessage } from '../../../lib/errors'
import { queryKeys } from '../../../lib/query-keys'
import { mePharmaciesApi } from './api'

function updatePharmacyFavoriteState(
  queryClient: QueryClient,
  pharmacyId: string,
  nextIsFavorite: boolean,
) {
  const now = new Date().toISOString()

  queryClient.setQueriesData<ConsumerPharmacyFeedItem[]>(
    { queryKey: ['personalization', 'pharmacies'] },
    (current) =>
      current?.map((item) =>
        item.pharmacyId === pharmacyId
          ? {
              ...item,
              isFavorite: nextIsFavorite,
              favoritedAtUtc: nextIsFavorite ? now : null,
            }
          : item,
      ),
  )
}

export function useTogglePharmacyFavorite() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ pharmacyId, isFavorite }: { pharmacyId: string; isFavorite: boolean }) => {
      if (isFavorite) {
        await mePharmaciesApi.removeFavorite(pharmacyId)
        return { pharmacyId, isFavorite: false }
      }

      await mePharmaciesApi.addFavorite(pharmacyId)
      return { pharmacyId, isFavorite: true }
    },
    onMutate: async ({ pharmacyId, isFavorite }) => {
      await queryClient.cancelQueries({ queryKey: queryKeys.personalization.pharmacies.all() })
      updatePharmacyFavoriteState(queryClient, pharmacyId, !isFavorite)
      return { pharmacyId, previousIsFavorite: isFavorite }
    },
    onSuccess: (result) => {
      toast.success(result.isFavorite ? 'Pharmacy added to favorites' : 'Pharmacy removed from favorites')
      queryClient.invalidateQueries({ queryKey: queryKeys.personalization.pharmacies.all() })
    },
    onError: (error, _variables, context) => {
      if (context) {
        updatePharmacyFavoriteState(queryClient, context.pharmacyId, context.previousIsFavorite)
      }

      toast.error(getApiErrorMessage(error, 'Unable to update pharmacy favorite'))
    },
  })
}
