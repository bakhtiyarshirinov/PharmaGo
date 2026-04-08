import { z } from 'zod'

export const pharmacySearchSchema = z.object({
  query: z.string().trim().optional().catch(''),
  page: z.coerce.number().int().min(1).default(1),
  pageSize: z.coerce.number().int().min(1).max(24).default(9),
})

export type PharmacySearchSchema = z.infer<typeof pharmacySearchSchema>
