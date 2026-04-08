import { z } from 'zod'

export const loginSchema = z.object({
  phoneNumber: z.string().min(7),
  password: z.string().min(8),
})
