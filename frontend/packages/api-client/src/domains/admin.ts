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
    medicines(params: { page?: number; pageSize?: number; search?: string } = {}) {
      return request<PagedResponse<Record<string, unknown>>>('/api/v1/admin/master-data/medicines', { query: params })
    },
    auditLogs(params: { pharmacyId?: string; entityName?: string; action?: string } = {}) {
      return request<AuditLogEntry[]>('/api/v1/auditlogs', { query: params })
    },
  }
}
