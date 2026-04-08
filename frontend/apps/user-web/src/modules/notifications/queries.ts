'use client'

import { useQuery } from '@tanstack/react-query'
import { notificationsApi } from './api'

export function useNotificationHistory(params: { page: number; pageSize: number; unreadOnly: boolean }) {
  return useQuery({
    queryKey: ['notifications', 'history', params],
    queryFn: () => notificationsApi.history(params),
  })
}

export function useNotificationUnread(previewLimit = 5) {
  return useQuery({
    queryKey: ['notifications', 'unread', previewLimit],
    queryFn: () => notificationsApi.unread(previewLimit),
    refetchInterval: 30_000,
  })
}

export function useNotificationPreferences() {
  return useQuery({
    queryKey: ['notifications', 'preferences'],
    queryFn: () => notificationsApi.preferences(),
  })
}
