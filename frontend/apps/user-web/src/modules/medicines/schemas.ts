import { z } from 'zod'

export const medicineSearchSchema = z.object({
  query: z.string().min(2).max(120),
})

