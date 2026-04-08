'use client'

import { useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { ApiError } from '@pharmago/api-client'
import type { NotificationPreferences } from '@pharmago/types'
import { notificationsApi } from './api'

function invalidate(queryClient: ReturnType<typeof useQueryClient>) {
  queryClient.invalidateQueries({ queryKey: ['notifications', 'history'] })
  queryClient.invalidateQueries({ queryKey: ['notifications', 'unread'] })
  queryClient.invalidateQueries({ queryKey: ['notifications', 'preferences'] })
}

function getErrorMessage(error: unknown) {
  return error instanceof ApiError ? error.details?.detail || error.message : 'Action failed'
}

export function useMarkNotificationRead() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (notificationId: string) => notificationsApi.markRead(notificationId),
    onSuccess: () => invalidate(queryClient),
    onError: (error) => toast.error(getErrorMessage(error)),
  })
}

export function useMarkNotificationUnread() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (notificationId: string) => notificationsApi.markUnread(notificationId),
    onSuccess: () => invalidate(queryClient),
    onError: (error) => toast.error(getErrorMessage(error)),
  })
}

export function useReadAllNotifications() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: () => notificationsApi.readAll(),
    onSuccess: () => {
      toast.success('All notifications marked as read')
      invalidate(queryClient)
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  })
}

export function useUpdateNotificationPreferences() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (input: Omit<NotificationPreferences, 'telegramLinked'>) => notificationsApi.updatePreferences(input),
    onSuccess: () => {
      toast.success('Notification preferences updated')
      invalidate(queryClient)
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  })
}
