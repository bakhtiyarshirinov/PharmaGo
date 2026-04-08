'use client'

import { useMutation, useQueryClient, type QueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import type { NotificationHistoryItem, NotificationInboxSummary, NotificationPreferences, PagedResponse } from '@pharmago/types'
import { getApiErrorMessage } from '../../lib/errors'
import { queryKeys } from '../../lib/query-keys'
import { notificationsApi } from './api'

function invalidate(queryClient: QueryClient) {
  queryClient.invalidateQueries({ queryKey: queryKeys.notifications.all() })
}

function updateNotificationReadState(
  queryClient: QueryClient,
  notificationId: string,
  isRead: boolean,
) {
  queryClient.setQueriesData<PagedResponse<NotificationHistoryItem>>(
    { queryKey: ['notifications', 'history'] },
    (current) => {
      if (!current) {
        return current
      }

      return {
        ...current,
        items: current.items.map((item) =>
          item.notificationId === notificationId
            ? {
                ...item,
                isRead,
                readAtUtc: isRead ? new Date().toISOString() : null,
              }
            : item,
        ),
      }
    },
  )

  queryClient.setQueriesData<NotificationInboxSummary>(
    { queryKey: ['notifications', 'unread'] },
    (current) => {
      if (!current) {
        return current
      }

      const previewItems = current.previewItems.map((item) =>
        item.notificationId === notificationId
          ? {
              ...item,
              isRead,
              readAtUtc: isRead ? new Date().toISOString() : null,
            }
          : item,
      )

      const unreadCount = Math.max(
        0,
        current.unreadCount + (isRead ? -1 : 1),
      )

      return {
        ...current,
        unreadCount,
        previewItems,
      }
    },
  )
}

export function useMarkNotificationRead() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (notificationId: string) => notificationsApi.markRead(notificationId),
    onMutate: async (notificationId) => {
      await queryClient.cancelQueries({ queryKey: ['notifications', 'history'] })
      await queryClient.cancelQueries({ queryKey: ['notifications', 'unread'] })
      updateNotificationReadState(queryClient, notificationId, true)
      return { notificationId }
    },
    onSuccess: () => invalidate(queryClient),
    onError: (error) => toast.error(getApiErrorMessage(error, 'Unable to mark notification as read')),
  })
}

export function useMarkNotificationUnread() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (notificationId: string) => notificationsApi.markUnread(notificationId),
    onMutate: async (notificationId) => {
      await queryClient.cancelQueries({ queryKey: ['notifications', 'history'] })
      await queryClient.cancelQueries({ queryKey: ['notifications', 'unread'] })
      updateNotificationReadState(queryClient, notificationId, false)
      return { notificationId }
    },
    onSuccess: () => invalidate(queryClient),
    onError: (error) => toast.error(getApiErrorMessage(error, 'Unable to mark notification as unread')),
  })
}

export function useReadAllNotifications() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: () => notificationsApi.readAll(),
    onMutate: async () => {
      await queryClient.cancelQueries({ queryKey: ['notifications', 'history'] })
      await queryClient.cancelQueries({ queryKey: ['notifications', 'unread'] })

      queryClient.setQueriesData<PagedResponse<NotificationHistoryItem>>(
        { queryKey: ['notifications', 'history'] },
        (current) =>
          current
            ? {
                ...current,
                items: current.items.map((item) => ({
                  ...item,
                  isRead: true,
                  readAtUtc: item.readAtUtc ?? new Date().toISOString(),
                })),
              }
            : current,
      )

      queryClient.setQueriesData<NotificationInboxSummary>(
        { queryKey: ['notifications', 'unread'] },
        (current) =>
          current
            ? {
                ...current,
                unreadCount: 0,
                previewItems: current.previewItems.map((item) => ({
                  ...item,
                  isRead: true,
                  readAtUtc: item.readAtUtc ?? new Date().toISOString(),
                })),
              }
            : current,
      )
    },
    onSuccess: () => {
      toast.success('All notifications marked as read')
      invalidate(queryClient)
    },
    onError: (error) => toast.error(getApiErrorMessage(error, 'Unable to mark all notifications as read')),
  })
}

export function useUpdateNotificationPreferences() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (input: Omit<NotificationPreferences, 'telegramLinked'>) => notificationsApi.updatePreferences(input),
    onMutate: async (input) => {
      await queryClient.cancelQueries({ queryKey: queryKeys.notifications.preferences() })
      const previous = queryClient.getQueryData<NotificationPreferences>(queryKeys.notifications.preferences())

      queryClient.setQueryData<NotificationPreferences | undefined>(queryKeys.notifications.preferences(), (current) =>
        current
          ? {
              ...current,
              ...input,
            }
          : current,
      )

      return { previous }
    },
    onSuccess: (nextPreferences) => {
      toast.success('Notification preferences updated')
      queryClient.setQueryData(queryKeys.notifications.preferences(), nextPreferences)
      invalidate(queryClient)
    },
    onError: (error, _input, context) => {
      if (context?.previous) {
        queryClient.setQueryData(queryKeys.notifications.preferences(), context.previous)
      }

      toast.error(getApiErrorMessage(error, 'Unable to update notification preferences'))
    },
  })
}
