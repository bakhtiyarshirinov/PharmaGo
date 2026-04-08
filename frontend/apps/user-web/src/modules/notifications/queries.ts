'use client'

import { useQuery } from '@tanstack/react-query'
import { queryKeys } from '../../lib/query-keys'
import { notificationsApi } from './api'

export function useNotificationHistory(params: { page: number; pageSize: number; unreadOnly: boolean }) {
  return useQuery({
    queryKey: queryKeys.notifications.history(params.page, params.pageSize, params.unreadOnly),
    queryFn: () => notificationsApi.history(params),
  })
}

export function useNotificationUnread(previewLimit = 5) {
  return useQuery({
    queryKey: queryKeys.notifications.unread(previewLimit),
    queryFn: () => notificationsApi.unread(previewLimit),
    refetchInterval: 30_000,
    staleTime: 15_000,
  })
}

export function useNotificationPreferences() {
  return useQuery({
    queryKey: queryKeys.notifications.preferences(),
    queryFn: () => notificationsApi.preferences(),
    staleTime: 60_000,
  })
}
