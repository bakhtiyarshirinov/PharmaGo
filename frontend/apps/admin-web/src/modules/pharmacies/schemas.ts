import { z } from 'zod'

export const pharmacyEditorSchema = z.object({
  name: z.string().trim().min(1, 'Укажи название аптеки.').max(200),
  address: z.string().trim().min(1, 'Укажи адрес.').max(400),
  city: z.string().trim().min(1, 'Укажи город.').max(100),
  region: z.string().trim().max(100).optional(),
  phoneNumber: z.string().trim().max(32).optional(),
  locationLatitude: z.union([z.coerce.number().min(-90).max(90), z.nan()]).optional(),
  locationLongitude: z.union([z.coerce.number().min(-180).max(180), z.nan()]).optional(),
  isOpen24Hours: z.boolean(),
  openingHoursJson: z.string().trim().optional(),
  supportsReservations: z.boolean(),
  hasDelivery: z.boolean(),
})

export type PharmacyEditorValues = z.infer<typeof pharmacyEditorSchema>
