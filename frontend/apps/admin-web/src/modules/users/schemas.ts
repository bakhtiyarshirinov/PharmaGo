import { z } from 'zod'

export const userCreateSchema = z.object({
  firstName: z.string().trim().min(1, 'Укажи имя.').max(100),
  lastName: z.string().trim().min(1, 'Укажи фамилию.').max(100),
  phoneNumber: z.string().trim().min(7, 'Укажи телефон.').max(32),
  email: z.string().trim().email('Невалидный email.').optional().or(z.literal('')),
  password: z.string().min(8, 'Минимум 8 символов.').max(128),
  telegramUsername: z.string().trim().max(100).optional(),
  telegramChatId: z.string().trim().max(100).optional(),
  role: z.coerce.number().refine((value) => value === 1 || value === 2, 'Выбери роль user или pharmacist.'),
  pharmacyId: z.string().uuid().optional().or(z.literal('')),
})

export const userUpdateSchema = userCreateSchema.extend({
  password: z.string().max(128).optional().or(z.literal('')),
})

export type UserCreateValues = z.infer<typeof userCreateSchema>
export type UserUpdateValues = z.infer<typeof userUpdateSchema>
