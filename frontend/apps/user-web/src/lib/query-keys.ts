export const queryKeys = {
  profile: {
    me: () => ['profile', 'me'] as const,
  },
  medicines: {
    search: (query: string) => ['medicines', 'search', query] as const,
    suggestions: (query: string) => ['medicines', 'suggestions', query] as const,
    popular: (limit: number) => ['medicines', 'popular', limit] as const,
    detail: (medicineId?: string) => ['medicines', 'detail', medicineId] as const,
    availability: (medicineId?: string) => ['medicines', 'availability', medicineId] as const,
  },
  pharmacies: {
    search: (input: Record<string, unknown>) => ['pharmacies', 'search', input] as const,
    suggestions: (query: string) => ['pharmacies', 'suggestions', query] as const,
    popular: (limit: number) => ['pharmacies', 'popular', limit] as const,
    detail: (pharmacyId?: string) => ['pharmacies', 'detail', pharmacyId] as const,
    medicines: (pharmacyId?: string, page = 1, pageSize = 12) => ['pharmacies', 'medicines', pharmacyId, page, pageSize] as const,
  },
  reservations: {
    all: () => ['reservations'] as const,
    mine: () => ['reservations', 'mine'] as const,
    detail: (reservationId?: string) => ['reservations', 'detail', reservationId] as const,
    timeline: (reservationId?: string) => ['reservations', 'timeline', reservationId] as const,
  },
  notifications: {
    all: () => ['notifications'] as const,
    history: (page: number, pageSize: number, unreadOnly: boolean) => ['notifications', 'history', page, pageSize, unreadOnly] as const,
    unread: (previewLimit: number) => ['notifications', 'unread', previewLimit] as const,
    preferences: () => ['notifications', 'preferences'] as const,
  },
  personalization: {
    medicines: {
      all: () => ['personalization', 'medicines'] as const,
      favorites: (limit: number) => ['personalization', 'medicines', 'favorites', limit] as const,
      recent: (limit: number) => ['personalization', 'medicines', 'recent', limit] as const,
    },
    pharmacies: {
      all: () => ['personalization', 'pharmacies'] as const,
      favorites: (limit: number) => ['personalization', 'pharmacies', 'favorites', limit] as const,
      recent: (limit: number) => ['personalization', 'pharmacies', 'recent', limit] as const,
    },
  },
} as const
