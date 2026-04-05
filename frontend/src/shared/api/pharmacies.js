import { apiRequest } from './client'

export function getPopularPharmacies(limit = 6) {
  return apiRequest(`/api/pharmacies/popular?limit=${limit}`)
}

export function searchPharmacies(query, pageSize = 8) {
  return apiRequest(`/api/pharmacies/search?query=${encodeURIComponent(query)}&page=1&pageSize=${pageSize}`)
}

export function getPharmacyDetail(id) {
  return apiRequest(`/api/pharmacies/${id}`)
}

export function getPharmacyMedicines(id, pageSize = 8) {
  return apiRequest(`/api/pharmacies/${id}/medicines?page=1&pageSize=${pageSize}`)
}

export function getFavoritePharmacies() {
  return apiRequest('/api/me/pharmacies/favorites')
}

export function getRecentPharmacies() {
  return apiRequest('/api/me/pharmacies/recent')
}

export function addFavoritePharmacy(id) {
  return apiRequest(`/api/me/pharmacies/favorites/${id}`, { method: 'POST' })
}

export function removeFavoritePharmacy(id) {
  return apiRequest(`/api/me/pharmacies/favorites/${id}`, { method: 'DELETE' })
}
