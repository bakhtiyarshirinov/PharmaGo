import { z } from 'zod'

export const notificationHistoryFiltersSchema = z.object({
  page: z.coerce.number().int().min(1).default(1),
  pageSize: z.coerce.number().int().min(5).max(50).default(10),
  unreadOnly: z.boolean().default(false),
})

export const notificationPreferencesSchema = z.object({
  inAppEnabled: z.boolean(),
  telegramEnabled: z.boolean(),
  reservationConfirmedEnabled: z.boolean(),
  reservationReadyEnabled: z.boolean(),
  reservationCancelledEnabled: z.boolean(),
  reservationExpiredEnabled: z.boolean(),
  reservationExpiringSoonEnabled: z.boolean(),
})

export type NotificationPreferencesFormValues = z.infer<typeof notificationPreferencesSchema>
