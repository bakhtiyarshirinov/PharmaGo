export const queryKeys = {
  overview: {
    summary: () => ['admin', 'overview', 'summary'] as const,
    recentReservations: () => ['admin', 'overview', 'recent-reservations'] as const,
    usersPreview: () => ['admin', 'overview', 'users-preview'] as const,
    pharmaciesPreview: () => ['admin', 'overview', 'pharmacies-preview'] as const,
  },
  pharmacies: {
    list: (params: {
      page: number
      pageSize: number
      search: string
      city: string
      isActive: string
      supportsReservations: string
    }) => ['admin', 'pharmacies', params] as const,
  },
  users: {
    list: (params: {
      page: number
      pageSize: number
      search: string
      role: string
      isActive: string
    }) => ['admin', 'users', params] as const,
  },
  audit: {
    list: (params: {
      pharmacyId: string
      entityName: string
      action: string
    }) => ['admin', 'audit', params] as const,
  },
} as const
