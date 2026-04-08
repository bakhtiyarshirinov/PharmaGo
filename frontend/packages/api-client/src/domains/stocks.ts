import type {
  ExpiringStockAlert,
  LowStockAlert,
  OutOfStockAlert,
  RestockSuggestion,
  StockItem,
} from '@pharmago/types'
import type { RequestOptions } from '../http/client'

export interface CreateStockItemInput {
  pharmacyId: string
  medicineId: string
  batchNumber: string
  expirationDate: string
  quantity: number
  purchasePrice: number
  retailPrice: number
  reorderLevel: number
}

export interface UpdateStockItemInput {
  batchNumber: string
  expirationDate: string
  quantity: number
  purchasePrice: number
  retailPrice: number
  reorderLevel: number
  isActive: boolean
}

export interface AdjustStockQuantityInput {
  quantityDelta: number
  reason: string
}

export interface ReceiveStockInput {
  quantityReceived: number
  purchasePrice?: number
  retailPrice?: number
  reorderLevel?: number
  reason?: string
}

export interface WriteOffStockInput {
  quantity: number
  reason: string
}

export function createStocksApi(request: <T>(path: string, options?: RequestOptions) => Promise<T>) {
  return {
    byPharmacy(pharmacyId: string, options?: { lowStockOnly?: boolean }) {
      return request<StockItem[]>(`/api/v1/stocks/pharmacy/${pharmacyId}`, {
        query: options?.lowStockOnly ? { lowStockOnly: true } : undefined,
      })
    },
    lowStock(pharmacyId?: string) {
      return request<LowStockAlert[]>('/api/v1/stocks/alerts/low-stock', {
        query: pharmacyId ? { pharmacyId } : undefined,
      })
    },
    outOfStock(pharmacyId?: string) {
      return request<OutOfStockAlert[]>('/api/v1/stocks/alerts/out-of-stock', {
        query: pharmacyId ? { pharmacyId } : undefined,
      })
    },
    expiring(days = 30, pharmacyId?: string) {
      return request<ExpiringStockAlert[]>('/api/v1/stocks/alerts/expiring', {
        query: {
          days,
          pharmacyId,
        },
      })
    },
    restockSuggestions(pharmacyId?: string) {
      return request<RestockSuggestion[]>('/api/v1/stocks/alerts/restock-suggestions', {
        query: pharmacyId ? { pharmacyId } : undefined,
      })
    },
    create(input: CreateStockItemInput) {
      return request<StockItem>('/api/v1/stocks', {
        method: 'POST',
        body: input,
      })
    },
    update(stockItemId: string, input: UpdateStockItemInput) {
      return request<StockItem>(`/api/v1/stocks/${stockItemId}`, {
        method: 'PUT',
        body: input,
      })
    },
    adjust(stockItemId: string, input: AdjustStockQuantityInput) {
      return request<StockItem>(`/api/v1/stocks/${stockItemId}/adjust`, {
        method: 'POST',
        body: input,
      })
    },
    receive(stockItemId: string, input: ReceiveStockInput) {
      return request<StockItem>(`/api/v1/stocks/${stockItemId}/receive`, {
        method: 'POST',
        body: input,
      })
    },
    writeoff(stockItemId: string, input: WriteOffStockInput) {
      return request<StockItem>(`/api/v1/stocks/${stockItemId}/writeoff`, {
        method: 'POST',
        body: input,
      })
    },
  }
}
