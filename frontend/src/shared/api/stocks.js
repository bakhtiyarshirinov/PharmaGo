import { apiRequest } from './client'

export function getPharmacyStock(pharmacyId) {
  return apiRequest(`/api/stocks/pharmacy/${pharmacyId}`)
}

export function getLowStockAlerts() {
  return apiRequest('/api/stocks/alerts/low-stock')
}

export function getOutOfStockAlerts() {
  return apiRequest('/api/stocks/alerts/out-of-stock')
}

export function getExpiringAlerts(days = 30) {
  return apiRequest(`/api/stocks/alerts/expiring?days=${days}`)
}

export function getRestockSuggestions() {
  return apiRequest('/api/stocks/alerts/restock-suggestions')
}
