export const queryKeys = {
  dashboard: {
    summary: (pharmacyId?: string | null) => ['dashboard', 'summary', pharmacyId ?? 'self'] as const,
    recentReservations: (pharmacyId?: string | null) => ['dashboard', 'recent-reservations', pharmacyId ?? 'self'] as const,
  },
  reservations: {
    all: () => ['reservations'] as const,
    active: (pharmacyId?: string | null) => ['reservations', 'active', pharmacyId ?? 'self'] as const,
    byPharmacy: (pharmacyId?: string | null, status?: number | 'closed' | null) =>
      ['reservations', 'by-pharmacy', pharmacyId ?? 'self', status ?? 'all'] as const,
    detail: (reservationId?: string) => ['reservations', 'detail', reservationId] as const,
    timeline: (reservationId?: string) => ['reservations', 'timeline', reservationId] as const,
  },
  inventory: {
    stock: (pharmacyId?: string | null, lowStockOnly = false) =>
      ['inventory', 'stock', pharmacyId ?? 'self', lowStockOnly ? 'low' : 'all'] as const,
    lowStock: (pharmacyId?: string | null) => ['inventory', 'low-stock', pharmacyId ?? 'self'] as const,
    outOfStock: (pharmacyId?: string | null) => ['inventory', 'out-of-stock', pharmacyId ?? 'self'] as const,
    expiring: (pharmacyId?: string | null, days = 30) => ['inventory', 'expiring', pharmacyId ?? 'self', days] as const,
    restock: (pharmacyId?: string | null) => ['inventory', 'restock', pharmacyId ?? 'self'] as const,
    medicineSuggestions: (query: string) => ['inventory', 'medicine-suggestions', query] as const,
  },
  notifications: {
    history: (page: number, pageSize: number, unreadOnly: boolean) =>
      ['notifications', 'history', page, pageSize, unreadOnly] as const,
    unread: (previewLimit: number) => ['notifications', 'unread', previewLimit] as const,
    preferences: () => ['notifications', 'preferences'] as const,
  },
} as const
