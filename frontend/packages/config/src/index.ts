export const backendUrl = process.env.NEXT_PUBLIC_API_BASE_URL ?? 'http://localhost:5122'

export type AuthPortal = 'user' | 'pharmacist' | 'admin'

export function getAuthCookieNames(portal: AuthPortal) {
  return {
    refreshToken: `pharmago_${portal}_refresh_token`,
    sessionMeta: `pharmago_${portal}_session_meta`,
  } as const
}

export const userRoutes = {
  home: '/',
  medicines: '/medicines',
  pharmacies: '/pharmacies',
  reservations: '/app/reservations',
  notifications: '/app/notifications',
  profile: '/app/profile',
  favoriteMedicines: '/app/medicines/favorites',
  recentMedicines: '/app/medicines/recent',
  favoritePharmacies: '/app/pharmacies/favorites',
  recentPharmacies: '/app/pharmacies/recent',
  login: '/auth/login',
  register: '/auth/register',
} as const

export const pharmacistRoutes = {
  home: '/cockpit',
  reservations: '/reservations',
  inventory: '/inventory',
  notifications: '/notifications',
  login: '/login',
} as const

export const adminRoutes = {
  home: '/overview',
  users: '/users',
  pharmacies: '/pharmacies',
  medicines: '/master-data/medicines',
  auditLogs: '/audit-logs',
  login: '/login',
} as const
