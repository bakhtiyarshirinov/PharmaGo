import { apiRequest } from './client'

export function getPopularMedicines(limit = 6) {
  return apiRequest(`/api/medicines/popular?limit=${limit}`)
}

export function searchMedicines(query, limit = 8, availabilityLimit = 4) {
  return apiRequest(
    `/api/medicines/search?query=${encodeURIComponent(query)}&limit=${limit}&availabilityLimit=${availabilityLimit}`,
  )
}

export function getMedicineDetail(id) {
  return apiRequest(`/api/medicines/${id}`)
}

export function getMedicineAvailability(id) {
  return apiRequest(`/api/medicines/${id}/availability`)
}

export function getMedicineSubstitutions(id, limit = 4) {
  return apiRequest(`/api/medicines/${id}/substitutions?limit=${limit}`)
}

export function getMedicineSimilar(id, limit = 4) {
  return apiRequest(`/api/medicines/${id}/similar?limit=${limit}`)
}

export function getFavoriteMedicines() {
  return apiRequest('/api/me/medicines/favorites')
}

export function getRecentMedicines() {
  return apiRequest('/api/me/medicines/recent')
}

export function addFavoriteMedicine(id) {
  return apiRequest(`/api/me/medicines/favorites/${id}`, { method: 'POST' })
}

export function removeFavoriteMedicine(id) {
  return apiRequest(`/api/me/medicines/favorites/${id}`, { method: 'DELETE' })
}
