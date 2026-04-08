'use client'

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import type { NotificationPreferences } from '@pharmago/types'
import { browserApi } from '../../lib/api'
import { getApiErrorMessage } from '../../lib/errors'
import { queryKeys } from '../../lib/query-keys'

export function useNotificationHistory(page: number, pageSize: number, unreadOnly: boolean) {
  return useQuery({
    queryKey: queryKeys.notifications.history(page, pageSize, unreadOnly),
    queryFn: () => browserApi.notifications.history({ page, pageSize, unreadOnly }),
  })
}

export function useNotificationUnread(previewLimit = 5) {
  return useQuery({
    queryKey: queryKeys.notifications.unread(previewLimit),
    queryFn: () => browserApi.notifications.unread(previewLimit),
  })
}

export function useNotificationPreferences() {
  return useQuery({
    queryKey: queryKeys.notifications.preferences(),
    queryFn: () => browserApi.notifications.preferences(),
  })
}

function useNotificationMutation<TInput>(
  successMessage: string,
  mutationFn: (input: TInput) => Promise<unknown>,
) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn,
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['notifications'] }),
        queryClient.invalidateQueries({ queryKey: ['dashboard'] }),
      ])

      toast.success(successMessage)
    },
    onError: (error) => {
      toast.error(getApiErrorMessage(error, 'Не удалось обновить уведомления.'))
    },
  })
}

export function useMarkNotificationRead() {
  return useNotificationMutation('Уведомление отмечено как прочитанное.', (notificationId: string) =>
    browserApi.notifications.markRead(notificationId))
}

export function useMarkNotificationUnread() {
  return useNotificationMutation('Уведомление снова стало непрочитанным.', (notificationId: string) =>
    browserApi.notifications.markUnread(notificationId))
}

export function useReadAllNotifications() {
  return useNotificationMutation('Все уведомления отмечены как прочитанные.', () => browserApi.notifications.readAll())
}

export function useUpdateNotificationPreferences() {
  return useNotificationMutation(
    'Настройки уведомлений сохранены.',
    (input: Omit<NotificationPreferences, 'telegramLinked'>) => browserApi.notifications.updatePreferences(input),
  )
}
