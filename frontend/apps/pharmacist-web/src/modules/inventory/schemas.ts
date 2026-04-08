import { z } from 'zod'

export const stockEditorSchema = z.object({
  medicineId: z.string().uuid('Выберите лекарство из подсказок.'),
  batchNumber: z.string().trim().min(1, 'Укажите номер партии.').max(64, 'Слишком длинный номер партии.'),
  expirationDate: z.string().min(1, 'Укажите срок годности.'),
  quantity: z.coerce.number().int().min(0, 'Количество не может быть отрицательным.'),
  purchasePrice: z.coerce.number().min(0, 'Закупочная цена не может быть отрицательной.'),
  retailPrice: z.coerce.number().min(0, 'Розничная цена не может быть отрицательной.'),
  reorderLevel: z.coerce.number().int().min(0, 'Точка дозакупки не может быть отрицательной.'),
  isActive: z.boolean(),
})

export type StockEditorValues = z.infer<typeof stockEditorSchema>

export const stockAdjustSchema = z.object({
  quantityDelta: z.coerce.number().int().refine((value) => value !== 0, 'Изменение должно отличаться от нуля.'),
  reason: z.string().trim().min(1, 'Укажите причину корректировки.').max(500),
})

export type StockAdjustValues = z.infer<typeof stockAdjustSchema>

export const stockReceiveSchema = z.object({
  quantityReceived: z.coerce.number().int().min(1, 'Количество должно быть больше нуля.'),
  purchasePrice: z.union([z.coerce.number().min(0), z.nan()]).optional(),
  retailPrice: z.union([z.coerce.number().min(0), z.nan()]).optional(),
  reorderLevel: z.union([z.coerce.number().int().min(0), z.nan()]).optional(),
  reason: z.string().trim().max(500).optional(),
})

export type StockReceiveValues = z.infer<typeof stockReceiveSchema>

export const stockWriteoffSchema = z.object({
  quantity: z.coerce.number().int().min(1, 'Количество должно быть больше нуля.'),
  reason: z.string().trim().min(1, 'Укажите причину списания.').max(500),
})

export type StockWriteoffValues = z.infer<typeof stockWriteoffSchema>
