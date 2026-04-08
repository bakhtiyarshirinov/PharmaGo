'use client'

import { useEffect, useState } from 'react'
import { useForm, type UseFormRegisterReturn } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { mapBackendRole, useAuthStore } from '@pharmago/auth/client'
import type { AuthSession } from '@pharmago/types'
import { Button, Card, CardContent, CardHeader, CardTitle, EmptyState, PageHeader, StatusBadge } from '@pharmago/ui'
import {
  formatDateTime,
  getNotificationChannelLabel,
  getNotificationEventLabel,
  getNotificationPreviewTone,
  getNotificationStatusMeta,
} from '../../lib/format'
import {
  useMarkNotificationRead,
  useMarkNotificationUnread,
  useNotificationHistory,
  useNotificationPreferences,
  useNotificationUnread,
  useReadAllNotifications,
  useUpdateNotificationPreferences,
} from './hooks'
import {
  notificationPreferencesSchema,
  type NotificationPreferencesValues,
} from './schemas'

interface NotificationsScreenProps {
  initialSession?: AuthSession | null
}

export function NotificationsScreen({ initialSession = null }: NotificationsScreenProps) {
  const liveSession = useAuthStore((state) => state.session)
  const session = liveSession ?? initialSession
  const role = session?.user.role ? mapBackendRole(session.user.role) : 'guest'
  const canManageReservations = role === 'pharmacist' || role === 'admin'
  const [page, setPage] = useState(1)
  const [unreadOnly, setUnreadOnly] = useState(false)
  const pageSize = 12

  const unread = useNotificationUnread(5)
  const history = useNotificationHistory(page, pageSize, unreadOnly)
  const preferences = useNotificationPreferences()
  const markRead = useMarkNotificationRead()
  const markUnread = useMarkNotificationUnread()
  const readAll = useReadAllNotifications()
  const updatePreferences = useUpdateNotificationPreferences()

  if (!canManageReservations) {
    return (
      <EmptyState
        title="Нет доступа к уведомлениям"
        description="Для этого рабочего inbox требуется доступ к операциям по резервам."
      />
    )
  }

  return (
    <div className="space-y-8">
      <PageHeader
        eyebrow="Ops inbox"
        title="История уведомлений по смене и контроль непрочитанного."
        description="Сначала непрочитанные сервисные события, затем полная история и персональные правила доставки."
        actions={
          <StatusBadge tone={(unread.data?.unreadCount ?? 0) > 0 ? 'info' : 'neutral'}>
            {unread.data?.unreadCount ?? 0} непрочитанных
          </StatusBadge>
        }
      />

      <div className="grid gap-6 xl:grid-cols-[1.15fr,0.85fr]">
        <Card>
          <CardHeader className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
            <div className="space-y-1">
              <CardTitle>История и inbox</CardTitle>
              <p className="text-sm text-slate-500">Фильтруйте только непрочитанные события и быстро переводите карточки в нужный статус.</p>
            </div>
            <div className="flex flex-wrap gap-2">
              <Button variant={unreadOnly ? 'primary' : 'outline'} size="sm" onClick={() => {
                setPage(1)
                setUnreadOnly((value) => !value)
              }}>
                {unreadOnly ? 'Показываем только непрочитанные' : 'Показать все'}
              </Button>
              <Button variant="outline" size="sm" disabled={readAll.isPending} onClick={() => readAll.mutate(undefined)}>
                Отметить все как прочитанные
              </Button>
            </div>
          </CardHeader>
          <CardContent className="space-y-4">
            {history.isLoading ? (
              <div className="space-y-3">
                {Array.from({ length: 5 }).map((_, index) => (
                  <div key={index} className="h-24 animate-pulse rounded-3xl bg-slate-100" />
                ))}
              </div>
            ) : history.isError ? (
              <div className="rounded-3xl border border-red-200 bg-red-50 p-5 text-sm text-red-700">
                Не удалось загрузить историю уведомлений.
              </div>
            ) : history.data?.items?.length ? (
              <>
                {history.data.items.map((notification) => {
                  const statusMeta = getNotificationStatusMeta(notification.status)

                  return (
                    <div key={notification.notificationId} className="rounded-3xl border border-slate-200 bg-slate-50 p-4">
                      <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
                        <div className="space-y-2">
                          <div className="flex flex-wrap items-center gap-2">
                            <StatusBadge tone={getNotificationPreviewTone(notification)}>
                              {notification.isRead ? 'Прочитано' : 'Новое'}
                            </StatusBadge>
                            <StatusBadge tone={statusMeta.tone}>{statusMeta.label}</StatusBadge>
                            <StatusBadge tone="neutral">{getNotificationChannelLabel(notification.channel)}</StatusBadge>
                          </div>
                          <div>
                            <p className="font-medium text-slate-950">{notification.title}</p>
                            <p className="mt-1 text-sm text-slate-500">{notification.message}</p>
                          </div>
                          <div className="text-xs text-slate-500">
                            {getNotificationEventLabel(notification.eventType)} · {formatDateTime(notification.createdAtUtc)}
                          </div>
                        </div>

                        <div className="flex flex-wrap gap-2">
                          {notification.isRead ? (
                            <Button variant="outline" size="sm" onClick={() => markUnread.mutate(notification.notificationId)}>
                              В непрочитанные
                            </Button>
                          ) : (
                            <Button variant="outline" size="sm" onClick={() => markRead.mutate(notification.notificationId)}>
                              Прочитано
                            </Button>
                          )}
                        </div>
                      </div>
                    </div>
                  )
                })}

                <div className="flex items-center justify-between rounded-3xl border border-slate-200 bg-white px-4 py-3">
                  <p className="text-sm text-slate-500">
                    Страница {history.data.page} из {history.data.totalPages}
                  </p>
                  <div className="flex gap-2">
                    <Button variant="outline" size="sm" disabled={history.data.page <= 1} onClick={() => setPage((value) => Math.max(1, value - 1))}>
                      Назад
                    </Button>
                    <Button
                      variant="outline"
                      size="sm"
                      disabled={history.data.page >= history.data.totalPages}
                      onClick={() => setPage((value) => value + 1)}
                    >
                      Дальше
                    </Button>
                  </div>
                </div>
              </>
            ) : (
              <EmptyState title="История пуста" description="Уведомления по резервам появятся здесь автоматически." />
            )}
          </CardContent>
        </Card>

        <div className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>Unread summary</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              {unread.data?.previewItems?.length ? (
                unread.data.previewItems.map((notification) => (
                  <div key={notification.notificationId} className="rounded-3xl bg-slate-50 p-4">
                    <div className="flex items-center justify-between gap-3">
                      <p className="font-medium text-slate-950">{notification.title}</p>
                      <StatusBadge tone="info">{getNotificationChannelLabel(notification.channel)}</StatusBadge>
                    </div>
                    <p className="mt-2 text-sm text-slate-500">{notification.message}</p>
                    <p className="mt-2 text-xs text-slate-400">{formatDateTime(notification.createdAtUtc)}</p>
                  </div>
                ))
              ) : (
                <EmptyState title="Inbox спокоен" description="Непрочитанных событий сейчас нет." />
              )}
            </CardContent>
          </Card>

          <NotificationPreferencesCard
            isLoading={preferences.isLoading}
            values={preferences.data}
            isSaving={updatePreferences.isPending}
            onSubmit={(values) => updatePreferences.mutate(values)}
          />
        </div>
      </div>
    </div>
  )
}

function NotificationPreferencesCard({
  values,
  isLoading,
  isSaving,
  onSubmit,
}: {
  values?: {
    inAppEnabled: boolean
    telegramEnabled: boolean
    telegramLinked: boolean
    reservationConfirmedEnabled: boolean
    reservationReadyEnabled: boolean
    reservationCancelledEnabled: boolean
    reservationExpiredEnabled: boolean
    reservationExpiringSoonEnabled: boolean
  }
  isLoading: boolean
  isSaving: boolean
  onSubmit: (values: NotificationPreferencesValues) => void
}) {
  const form = useForm<NotificationPreferencesValues>({
    resolver: zodResolver(notificationPreferencesSchema),
    defaultValues: {
      inAppEnabled: true,
      telegramEnabled: false,
      reservationConfirmedEnabled: true,
      reservationReadyEnabled: true,
      reservationCancelledEnabled: true,
      reservationExpiredEnabled: true,
      reservationExpiringSoonEnabled: true,
    },
  })

  useEffect(() => {
    if (values) {
      form.reset({
        inAppEnabled: values.inAppEnabled,
        telegramEnabled: values.telegramEnabled,
        reservationConfirmedEnabled: values.reservationConfirmedEnabled,
        reservationReadyEnabled: values.reservationReadyEnabled,
        reservationCancelledEnabled: values.reservationCancelledEnabled,
        reservationExpiredEnabled: values.reservationExpiredEnabled,
        reservationExpiringSoonEnabled: values.reservationExpiringSoonEnabled,
      })
    }
  }, [form, values])

  return (
    <Card>
      <CardHeader>
        <CardTitle>Настройки доставки</CardTitle>
      </CardHeader>
      <CardContent>
        {isLoading ? (
          <div className="space-y-3">
            {Array.from({ length: 4 }).map((_, index) => (
              <div key={index} className="h-12 animate-pulse rounded-3xl bg-slate-100" />
            ))}
          </div>
        ) : (
          <form className="space-y-3" onSubmit={form.handleSubmit(onSubmit)}>
            <CheckboxField
              label="In-app уведомления"
              description="Основной канал для рабочего inbox внутри портала."
              registration={form.register('inAppEnabled')}
            />
            <CheckboxField
              label="Telegram"
              description={values?.telegramLinked ? 'Telegram уже привязан к аккаунту.' : 'Telegram не привязан, канал можно включить заранее.'}
              registration={form.register('telegramEnabled')}
            />
            <CheckboxField
              label="Подтверждение резерва"
              description="События о переводе заказа в подтвержденный статус."
              registration={form.register('reservationConfirmedEnabled')}
            />
            <CheckboxField
              label="Готов к выдаче"
              description="События о подготовке заказа к выдаче."
              registration={form.register('reservationReadyEnabled')}
            />
            <CheckboxField
              label="Отмена"
              description="Уведомления об отмененных заказах."
              registration={form.register('reservationCancelledEnabled')}
            />
            <CheckboxField
              label="Истечение срока"
              description="События о резервах, которые expired."
              registration={form.register('reservationExpiredEnabled')}
            />
            <CheckboxField
              label="Скорое истечение"
              description="Напоминания о резервах, которые скоро истекут."
              registration={form.register('reservationExpiringSoonEnabled')}
            />

            <Button className="w-full" type="submit" disabled={isSaving}>
              {isSaving ? 'Сохраняем...' : 'Сохранить настройки'}
            </Button>
          </form>
        )}
      </CardContent>
    </Card>
  )
}

function CheckboxField({
  label,
  description,
  registration,
}: {
  label: string
  description: string
  registration: UseFormRegisterReturn
}) {
  return (
    <label className="flex items-start gap-3 rounded-3xl bg-slate-50 px-4 py-3 text-sm text-slate-700">
      <input type="checkbox" className="mt-1 h-4 w-4 rounded border-slate-300" {...registration} />
      <span>
        <span className="font-medium text-slate-900">{label}</span>
        <span className="mt-1 block text-slate-500">{description}</span>
      </span>
    </label>
  )
}
