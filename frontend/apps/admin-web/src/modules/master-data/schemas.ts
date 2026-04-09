import { z } from 'zod'

export const medicineEditorSchema = z.object({
  brandName: z.string().trim().min(1, 'Укажи бренд.').max(200),
  genericName: z.string().trim().min(1, 'Укажи дженерик.').max(200),
  description: z.string().trim().max(2000).optional(),
  dosageForm: z.string().trim().min(1, 'Укажи лекарственную форму.').max(100),
  strength: z.string().trim().min(1, 'Укажи дозировку.').max(100),
  manufacturer: z.string().trim().min(1, 'Укажи производителя.').max(200),
  countryOfOrigin: z.string().trim().max(100).optional(),
  barcode: z.string().trim().max(64).optional(),
  requiresPrescription: z.boolean(),
  isActive: z.boolean(),
  categoryId: z.string().uuid().or(z.literal('')).optional(),
})

export type MedicineEditorValues = z.infer<typeof medicineEditorSchema>

export const medicineCategoryEditorSchema = z.object({
  name: z.string().trim().min(1, 'Укажи название категории.').max(150),
  description: z.string().trim().max(1000).optional(),
})

export type MedicineCategoryEditorValues = z.infer<typeof medicineCategoryEditorSchema>
