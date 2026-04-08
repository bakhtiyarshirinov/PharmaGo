import type { MedicineAvailabilityItem, MedicineDetail, MedicineSearchItem } from '@pharmago/types'

export type MedicineSearchResult = MedicineSearchItem
export type MedicineDetailModel = MedicineDetail
export type MedicineAvailabilityModel = MedicineAvailabilityItem

export interface MedicineSuggestion {
  medicineId: string
  brandName: string
  genericName: string
}
