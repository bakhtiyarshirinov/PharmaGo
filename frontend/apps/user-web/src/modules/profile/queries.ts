'use client'

import { useQuery } from '@tanstack/react-query'
import { browserApi } from '../../lib/api'
import { queryKeys } from '../../lib/query-keys'

export function useProfile() {
  return useQuery({
    queryKey: queryKeys.profile.me(),
    queryFn: () => browserApi.auth.me(),
    staleTime: 60_000,
  })
}
