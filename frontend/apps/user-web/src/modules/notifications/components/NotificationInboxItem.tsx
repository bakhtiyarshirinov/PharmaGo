import Link from 'next/link'
import type { NotificationHistoryItem } from '@pharmago/types'
import { Button, Card, CardContent, StatusBadge } from '@pharmago/ui'
import { getNotificationEventLabel, getNotificationStatusLabel, getNotificationStatusTone } from '../../../lib/domain'
import { formatDateTime, formatRelativeTime } from '../../../lib/format'

export interface NotificationInboxItemProps {
  notification: NotificationHistoryItem
  onRead: (notificationId: string) => void
  onUnread: (notificationId: string) => void
}

export function NotificationInboxItem({ notification, onRead, onUnread }: NotificationInboxItemProps) {
  return (
    <Card className={notification.isRead ? 'border-slate-200' : 'border-emerald-200 shadow-[0_12px_40px_-24px_rgba(5,150,105,0.55)]'}>
      <CardContent className="space-y-4 p-5">
        <div className="flex flex-col gap-3 md:flex-row md:items-start md:justify-between">
          <div className="space-y-1">
            <div className="flex flex-wrap items-center gap-2">
              <StatusBadge tone={notification.isRead ? 'neutral' : 'success'}>
                {notification.isRead ? 'Read' : 'Unread'}
              </StatusBadge>
              <StatusBadge tone={getNotificationStatusTone(notification.status)}>
                {getNotificationStatusLabel(notification.status)}
              </StatusBadge>
            </div>
            <h3 className="text-base font-semibold text-slate-950">{notification.title}</h3>
            <p className="text-sm text-slate-500">{getNotificationEventLabel(notification.eventType)}</p>
          </div>
          <div className="text-right text-xs uppercase tracking-wide text-slate-400">
            <p>{formatRelativeTime(notification.createdAtUtc)}</p>
            <p>{formatDateTime(notification.createdAtUtc)}</p>
          </div>
        </div>

        <p className="text-sm leading-6 text-slate-700">{notification.message}</p>

        <div className="flex flex-wrap items-center gap-3">
          {notification.reservationId ? (
            <Button asChild variant="outline" size="sm">
              <Link href={`/app/reservations/${notification.reservationId}`}>Open reservation</Link>
            </Button>
          ) : null}

          {notification.isRead ? (
            <Button variant="ghost" size="sm" onClick={() => onUnread(notification.notificationId)}>
              Mark unread
            </Button>
          ) : (
            <Button variant="ghost" size="sm" onClick={() => onRead(notification.notificationId)}>
              Mark read
            </Button>
          )}
        </div>
      </CardContent>
    </Card>
  )
}
