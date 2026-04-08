'use client'

import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Button, Card, CardContent, CardHeader, CardTitle, StatusBadge } from '@pharmago/ui'
import type { NotificationPreferences } from '@pharmago/types'
import { notificationPreferencesSchema, type NotificationPreferencesFormValues } from '../schemas'

export interface NotificationPreferencesFormProps {
  preferences: NotificationPreferences
  isSaving?: boolean
  onSubmit: (values: Omit<NotificationPreferences, 'telegramLinked'>) => void
}

const preferenceFields: Array<{
  name: keyof NotificationPreferencesFormValues
  title: string
  description: string
}> = [
  { name: 'inAppEnabled', title: 'In-app inbox', description: 'Show notification events inside the app inbox.' },
  { name: 'telegramEnabled', title: 'Telegram', description: 'Keep this off for MVP if the channel is not linked yet.' },
  { name: 'reservationConfirmedEnabled', title: 'Confirmed reservations', description: 'Notify when staff confirms your reservation.' },
  { name: 'reservationReadyEnabled', title: 'Ready for pickup', description: 'Notify when the pharmacy marks an order ready.' },
  { name: 'reservationCancelledEnabled', title: 'Cancelled reservations', description: 'Notify when a reservation is cancelled.' },
  { name: 'reservationExpiredEnabled', title: 'Expired reservations', description: 'Notify when a reservation expires.' },
  { name: 'reservationExpiringSoonEnabled', title: 'Expiring soon reminders', description: 'Send reminders 45, 30 and 15 minutes before expiry.' },
]

export function NotificationPreferencesForm({ preferences, isSaving = false, onSubmit }: NotificationPreferencesFormProps) {
  const form = useForm<NotificationPreferencesFormValues>({
    resolver: zodResolver(notificationPreferencesSchema),
    defaultValues: {
      inAppEnabled: preferences.inAppEnabled,
      telegramEnabled: preferences.telegramEnabled,
      reservationConfirmedEnabled: preferences.reservationConfirmedEnabled,
      reservationReadyEnabled: preferences.reservationReadyEnabled,
      reservationCancelledEnabled: preferences.reservationCancelledEnabled,
      reservationExpiredEnabled: preferences.reservationExpiredEnabled,
      reservationExpiringSoonEnabled: preferences.reservationExpiringSoonEnabled,
    },
  })

  useEffect(() => {
    form.reset({
      inAppEnabled: preferences.inAppEnabled,
      telegramEnabled: preferences.telegramEnabled,
      reservationConfirmedEnabled: preferences.reservationConfirmedEnabled,
      reservationReadyEnabled: preferences.reservationReadyEnabled,
      reservationCancelledEnabled: preferences.reservationCancelledEnabled,
      reservationExpiredEnabled: preferences.reservationExpiredEnabled,
      reservationExpiringSoonEnabled: preferences.reservationExpiringSoonEnabled,
    })
  }, [form, preferences])

  return (
    <Card>
      <CardHeader className="flex flex-row items-start justify-between gap-4">
        <div className="space-y-1">
          <CardTitle>Notification preferences</CardTitle>
          <p className="text-sm text-slate-500">Control how reservation events reach you during the MVP period.</p>
        </div>
        <StatusBadge tone={preferences.telegramLinked ? 'success' : 'neutral'}>
          {preferences.telegramLinked ? 'Telegram linked' : 'Telegram not linked'}
        </StatusBadge>
      </CardHeader>
      <CardContent>
        <form className="space-y-4" onSubmit={form.handleSubmit((values) => onSubmit(values))}>
          <div className="space-y-3">
            {preferenceFields.map((field) => (
              <label key={field.name} className="flex items-start gap-3 rounded-2xl border border-slate-200 p-4">
                <input
                  type="checkbox"
                  className="mt-1 h-4 w-4 rounded border-slate-300 text-emerald-700 focus:ring-emerald-600"
                  {...form.register(field.name)}
                />
                <span className="space-y-1">
                  <span className="block text-sm font-medium text-slate-900">{field.title}</span>
                  <span className="block text-sm text-slate-500">{field.description}</span>
                </span>
              </label>
            ))}
          </div>

          <div className="flex justify-end">
            <Button type="submit" disabled={isSaving}>
              Save preferences
            </Button>
          </div>
        </form>
      </CardContent>
    </Card>
  )
}
