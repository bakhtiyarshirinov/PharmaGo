'use client'

import { useMemo } from 'react'
import { useRouter, useSearchParams } from 'next/navigation'
import { Button, Card, CardContent, CardHeader, CardTitle, EmptyState, PageHeader, StatusBadge } from '@pharmago/ui'
import { NotificationInboxItem } from '../../../../modules/notifications/components/NotificationInboxItem'
import { NotificationPreferencesForm } from '../../../../modules/notifications/components/NotificationPreferencesForm'
import {
  useMarkNotificationRead,
  useMarkNotificationUnread,
  useReadAllNotifications,
  useUpdateNotificationPreferences,
} from '../../../../modules/notifications/mutations'
import { useNotificationHistory, useNotificationPreferences, useNotificationUnread } from '../../../../modules/notifications/queries'
import { notificationHistoryFiltersSchema } from '../../../../modules/notifications/schemas'

export default function NotificationsPage() {
  const router = useRouter()
  const searchParams = useSearchParams()

  const filters = useMemo(
    () =>
      notificationHistoryFiltersSchema.parse({
        page: searchParams.get('page') ?? '1',
        pageSize: searchParams.get('pageSize') ?? '10',
        unreadOnly: searchParams.get('unreadOnly') === 'true',
      }),
    [searchParams],
  )

  const unread = useNotificationUnread()
  const history = useNotificationHistory(filters)
  const preferences = useNotificationPreferences()
  const markRead = useMarkNotificationRead()
  const markUnread = useMarkNotificationUnread()
  const readAll = useReadAllNotifications()
  const updatePreferences = useUpdateNotificationPreferences()

  return (
    <div className="space-y-8">
      <PageHeader
        eyebrow="Notification inbox"
        title="Reservation updates without guesswork."
        description="Unread summary, reminder pressure and notification preferences are grouped here so the reservation experience stays trustworthy."
        actions={
          <StatusBadge tone={unread.data?.unreadCount ? 'success' : 'neutral'}>
            {unread.data?.unreadCount ?? 0} unread
          </StatusBadge>
        }
      />

      <div className="grid gap-6 xl:grid-cols-[1.2fr,0.8fr]">
        <div className="space-y-6">
          <Card className="consumer-glass border-white/70">
            <CardHeader className="flex flex-row items-start justify-between gap-4">
              <div className="space-y-1">
                <CardTitle>Inbox</CardTitle>
                <p className="text-sm text-slate-500">Latest reservation events across confirmations, reminders and cancellations.</p>
              </div>
              <div className="flex flex-wrap gap-2">
                <Button
                  variant={filters.unreadOnly ? 'primary' : 'outline'}
                  size="sm"
                  onClick={() => {
                    const next = new URLSearchParams(searchParams.toString())
                    next.set('unreadOnly', String(!filters.unreadOnly))
                    next.set('page', '1')
                    router.push(`/app/notifications?${next.toString()}`)
                  }}
                >
                  {filters.unreadOnly ? 'Show all' : 'Unread only'}
                </Button>
                <Button variant="outline" size="sm" disabled={readAll.isPending} onClick={() => readAll.mutate()}>
                  Mark all read
                </Button>
              </div>
            </CardHeader>
            <CardContent className="space-y-4">
              {history.data?.items?.length ? (
                <>
                  {history.data.items.map((notification) => (
                    <NotificationInboxItem
                      key={notification.notificationId}
                      notification={notification}
                      onRead={(notificationId) => markRead.mutate(notificationId)}
                      onUnread={(notificationId) => markUnread.mutate(notificationId)}
                    />
                  ))}

                  <div className="flex items-center justify-between rounded-[1.75rem] border border-white/70 bg-white/80 px-4 py-3 shadow-[0_18px_50px_rgba(148,163,184,0.12)]">
                    <p className="text-sm text-slate-500">
                      Page {history.data.page} of {history.data.totalPages}
                    </p>
                    <div className="flex gap-2">
                      <Button
                        variant="outline"
                        size="sm"
                        disabled={history.data.page <= 1}
                        onClick={() => {
                          const next = new URLSearchParams(searchParams.toString())
                          next.set('page', String(history.data!.page - 1))
                          router.push(`/app/notifications?${next.toString()}`)
                        }}
                      >
                        Previous
                      </Button>
                      <Button
                        variant="outline"
                        size="sm"
                        disabled={history.data.page >= history.data.totalPages}
                        onClick={() => {
                          const next = new URLSearchParams(searchParams.toString())
                          next.set('page', String(history.data!.page + 1))
                          router.push(`/app/notifications?${next.toString()}`)
                        }}
                      >
                        Next
                      </Button>
                    </div>
                  </div>
                </>
              ) : (
                <EmptyState title="No notifications yet" description="Reservation lifecycle events will appear here as soon as you start using the app." />
              )}
            </CardContent>
          </Card>
        </div>

        <div className="space-y-6">
          <Card className="consumer-glass border-white/70">
            <CardHeader>
              <CardTitle>Unread preview</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              {unread.data?.previewItems?.length ? (
                unread.data.previewItems.map((notification) => (
                  <div
                    key={notification.notificationId}
                    className="rounded-[1.75rem] border border-white/70 bg-white/80 p-4 shadow-[0_18px_50px_rgba(148,163,184,0.12)]"
                  >
                    <p className="text-sm font-medium text-slate-950">{notification.title}</p>
                    <p className="mt-1 text-sm text-slate-500">{notification.message}</p>
                  </div>
                ))
              ) : (
                <EmptyState title="Inbox is calm" description="Unread preview cards will appear here when new reservation events arrive." />
              )}
            </CardContent>
          </Card>

          {preferences.data ? (
            <NotificationPreferencesForm
              preferences={preferences.data}
              isSaving={updatePreferences.isPending}
              onSubmit={(values) => updatePreferences.mutate(values)}
            />
          ) : null}
        </div>
      </div>
    </div>
  )
}
