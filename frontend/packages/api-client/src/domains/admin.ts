import type { AuditLogEntry, ManagedPharmacy, ManagedUser, PagedResponse } from '@pharmago/types'
import type { RequestOptions } from '../http/client'

export function createAdminApi(request: <T>(path: string, options?: RequestOptions) => Promise<T>) {
  return {
    users(params: {
      page?: number
      pageSize?: number
      search?: string
      role?: number
      isActive?: boolean
      pharmacyId?: string
      sortBy?: string
      sortDirection?: 'asc' | 'desc'
    } = {}) {
      return request<PagedResponse<ManagedUser>>('/api/v1/users', { query: params })
    },
    pharmacies(params: {
      page?: number
      pageSize?: number
      search?: string
      city?: string
      isActive?: boolean
      supportsReservations?: boolean
      hasDelivery?: boolean
      sortBy?: string
      sortDirection?: 'asc' | 'desc'
    } = {}) {
      return request<PagedResponse<ManagedPharmacy>>('/api/v1/admin/pharmacies', { query: params })
    },
    createPharmacy(input: {
      name: string
      address: string
      city: string
      region?: string | null
      phoneNumber?: string | null
      locationLatitude?: number | null
      locationLongitude?: number | null
      isOpen24Hours: boolean
      openingHoursJson?: string | null
      supportsReservations: boolean
      hasDelivery: boolean
      pharmacyChainId?: string | null
    }) {
      return request<ManagedPharmacy>('/api/v1/admin/pharmacies', {
        method: 'POST',
        body: input,
      })
    },
    updatePharmacy(
      pharmacyId: string,
      input: {
        name: string
        address: string
        city: string
        region?: string | null
        phoneNumber?: string | null
        locationLatitude?: number | null
        locationLongitude?: number | null
        isOpen24Hours: boolean
        openingHoursJson?: string | null
        supportsReservations: boolean
        hasDelivery: boolean
        pharmacyChainId?: string | null
      },
    ) {
      return request<ManagedPharmacy>(`/api/v1/admin/pharmacies/${pharmacyId}`, {
        method: 'PUT',
        body: input,
      })
    },
    deactivatePharmacy(pharmacyId: string) {
      return request<void>(`/api/v1/admin/pharmacies/${pharmacyId}`, {
        method: 'DELETE',
      })
    },
    restorePharmacy(pharmacyId: string) {
      return request<ManagedPharmacy>(`/api/v1/admin/pharmacies/${pharmacyId}/restore`, {
        method: 'POST',
      })
    },
    medicines(params: { page?: number; pageSize?: number; search?: string } = {}) {
      return request<PagedResponse<Record<string, unknown>>>('/api/v1/admin/master-data/medicines', { query: params })
    },
    auditLogs(params: { pharmacyId?: string; entityName?: string; action?: string } = {}) {
      return request<AuditLogEntry[]>('/api/v1/auditlogs', { query: params })
    },
    createUser(input: {
      firstName: string
      lastName: string
      phoneNumber: string
      email?: string | null
      password: string
      telegramUsername?: string | null
      telegramChatId?: string | null
      role: number
      pharmacyId?: string | null
    }) {
      return request<ManagedUser>('/api/v1/users', {
        method: 'POST',
        body: input,
      })
    },
    updateUser(
      userId: string,
      input: {
        firstName: string
        lastName: string
        phoneNumber: string
        email?: string | null
        password?: string | null
        telegramUsername?: string | null
        telegramChatId?: string | null
        role: number
        pharmacyId?: string | null
      },
    ) {
      return request<ManagedUser>(`/api/v1/users/${userId}`, {
        method: 'PUT',
        body: input,
      })
    },
    deactivateUser(userId: string) {
      return request<void>(`/api/v1/users/${userId}`, {
        method: 'DELETE',
      })
    },
    restoreUser(userId: string) {
      return request<ManagedUser>(`/api/v1/users/${userId}/restore`, {
        method: 'POST',
      })
    },
  }
}
