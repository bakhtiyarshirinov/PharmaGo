import { z } from 'zod'

export const notificationPreferencesSchema = z.object({
  inAppEnabled: z.boolean(),
  telegramEnabled: z.boolean(),
  reservationConfirmedEnabled: z.boolean(),
  reservationReadyEnabled: z.boolean(),
  reservationCancelledEnabled: z.boolean(),
  reservationExpiredEnabled: z.boolean(),
  reservationExpiringSoonEnabled: z.boolean(),
})

export type NotificationPreferencesValues = z.infer<typeof notificationPreferencesSchema>
