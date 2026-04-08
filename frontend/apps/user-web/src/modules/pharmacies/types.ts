export interface PharmacySearchFilters {
  query?: string
  latitude?: number
  longitude?: number
  page: number
  pageSize: number
}

export interface PharmacySuggestion {
  pharmacyId: string
  name: string
  city?: string
}
