import { z } from 'zod'

export const createReservationSchema = z.object({
  pharmacyId: z.string().uuid(),
  reserveForHours: z.number().min(1).max(24),
  notes: z.string().max(1000).optional(),
  items: z.array(
    z.object({
      medicineId: z.string().uuid(),
      quantity: z.number().min(1),
    }),
  ).min(1),
})

