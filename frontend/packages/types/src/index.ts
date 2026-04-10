export type Role = 'guest' | 'consumer' | 'pharmacist' | 'admin'

export interface ApiProblemDetails {
  type?: string
  title: string
  status: number
  detail?: string
  code?: string
  errors?: Record<string, string[]>
}

export interface PagedResponse<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  sortBy?: string
  sortDirection?: 'asc' | 'desc'
}

export interface UserProfile {
  id: string
  firstName: string
  lastName: string
  phoneNumber: string
  email?: string | null
  telegramUsername?: string | null
  telegramChatId?: string | null
  role: number
  pharmacyId?: string | null
}

export interface AuthSession {
  accessToken: string
  expiresAtUtc: string
  user: UserProfile
}

export interface AuthResponse extends AuthSession {
  refreshToken: string
  refreshTokenExpiresAtUtc: string
}

export interface MedicineSearchItem {
  medicineId: string
  brandName: string
  genericName: string
  dosageForm: string
  strength: string
  manufacturer: string
  requiresPrescription: boolean
  pharmacyCount: number
  totalAvailableQuantity: number
  minRetailPrice?: number | null
  nearestDistanceKm?: number | null
  availabilities?: MedicineAvailabilityItem[]
}

export interface MedicineAvailabilityItem {
  pharmacyId: string
  pharmacyName: string
  city?: string | null
  address?: string | null
  retailPrice?: number | null
  availableQuantity: number
  supportsReservations: boolean
  isOpenNow?: boolean
}

export interface MedicineDetail {
  medicineId: string
  brandName: string
  genericName: string
  description?: string | null
  dosageForm: string
  strength: string
  manufacturer: string
  countryOfOrigin?: string | null
  categoryName?: string | null
  requiresPrescription: boolean
  pharmacyCount: number
  totalAvailableQuantity: number
  minRetailPrice?: number | null
}

export interface ConsumerMedicineFeedItem {
  medicineId: string
  brandName: string
  genericName: string
  dosageForm: string
  strength: string
  manufacturer: string
  requiresPrescription: boolean
  categoryId?: string | null
  categoryName?: string | null
  pharmacyCount: number
  totalAvailableQuantity: number
  minRetailPrice?: number | null
  hasAvailability: boolean
  isFavorite: boolean
  favoritedAtUtc?: string | null
  lastViewedAtUtc?: string | null
  popularityScore?: number | null
}

export interface PharmacyDetail {
  pharmacyId: string
  name: string
  chainName?: string | null
  address: string
  city: string
  region?: string | null
  phoneNumber?: string | null
  locationLatitude?: number | null
  locationLongitude?: number | null
  distanceKm?: number | null
  isOpen24Hours: boolean
  isOpenNow: boolean
  openingHoursJson?: string | null
  supportsReservations: boolean
  hasDelivery: boolean
  availableMedicineCount: number
  totalAvailableUnits: number
  minAvailablePrice?: number | null
}

export interface ConsumerPharmacyFeedItem {
  pharmacyId: string
  name: string
  chainName?: string | null
  address: string
  city: string
  region?: string | null
  phoneNumber?: string | null
  isOpen24Hours: boolean
  supportsReservations: boolean
  hasDelivery: boolean
  availableMedicineCount: number
  totalAvailableUnits: number
  minAvailablePrice?: number | null
  isFavorite: boolean
  favoritedAtUtc?: string | null
  lastViewedAtUtc?: string | null
  popularityScore?: number | null
}

export interface PharmacyMedicineItem {
  medicineId: string
  brandName: string
  genericName: string
  dosageForm?: string | null
  strength?: string | null
  retailPrice?: number | null
  availableQuantity: number
  isReservable: boolean
}

export interface ReservationItem {
  medicineId: string
  medicineName: string
  genericName?: string | null
  quantity: number
  unitPrice: number
  totalPrice?: number
}

export interface Reservation {
  reservationId: string
  reservationNumber: string
  status: number
  pharmacyId: string
  pharmacyName: string
  customerId: string
  customerFullName: string
  phoneNumber: string
  createdAtUtc: string
  reservedUntilUtc: string
  pickupAvailableFromUtc?: string | null
  confirmedAtUtc?: string | null
  readyForPickupAtUtc?: string | null
  completedAtUtc?: string | null
  cancelledAtUtc?: string | null
  expiredAtUtc?: string | null
  totalAmount: number
  notes?: string | null
  items: ReservationItem[]
}

export interface ReservationTimelineEvent {
  title: string
  description?: string | null
  action?: string | null
  status?: number | null
  occurredAtUtc: string
  userId?: string | null
  userFullName?: string | null
  isSystemEvent?: boolean
}

export interface NotificationHistoryItem {
  notificationId: string
  eventType: number
  channel: number
  status: number
  reservationId?: string | null
  title: string
  message: string
  createdAtUtc: string
  deliveredAtUtc?: string | null
  readAtUtc?: string | null
  isRead: boolean
}

export interface NotificationInboxSummary {
  unreadCount: number
  latestUnread?: NotificationHistoryItem | null
  previewItems: NotificationHistoryItem[]
}

export interface NotificationPreferences {
  inAppEnabled: boolean
  telegramEnabled: boolean
  telegramLinked: boolean
  reservationConfirmedEnabled: boolean
  reservationReadyEnabled: boolean
  reservationCancelledEnabled: boolean
  reservationExpiredEnabled: boolean
  reservationExpiringSoonEnabled: boolean
}

export interface StockItem {
  id?: string
  stockItemId?: string
  medicineId: string
  medicineName: string
  genericName?: string | null
  pharmacyId: string
  pharmacyName?: string | null
  batchNumber: string
  quantity: number
  reservedQuantity: number
  availableQuantity: number
  purchasePrice?: number | null
  retailPrice?: number | null
  reorderLevel?: number
  expirationDate: string
  isReservable: boolean
  isLowStock?: boolean
  isActive?: boolean
  lastStockUpdatedAtUtc?: string | null
}

export interface DashboardSummary {
  pharmacyId?: string | null
  pharmacyName?: string | null
  totalMedicines: number
  totalPharmacies: number
  totalUsers: number
  totalStockItems: number
  totalAvailableUnits: number
  totalReservedUnits: number
  activeReservations: number
  readyForPickupReservations: number
  lowStockAlerts: number
  completedToday: number
  reservedValue: number
}

export interface DashboardRecentReservation {
  reservationId: string
  reservationNumber: string
  status: number
  customerFullName: string
  pharmacyName: string
  totalAmount: number
  reservedUntilUtc: string
  createdAtUtc: string
}

export interface LowStockAlert {
  stockItemId: string
  pharmacyId: string
  pharmacyName: string
  medicineId: string
  medicineName: string
  genericName: string
  batchNumber: string
  expirationDate: string
  quantity: number
  reservedQuantity: number
  availableQuantity: number
  reorderLevel: number
  deficit: number
  retailPrice: number
}

export interface OutOfStockAlert {
  pharmacyId: string
  pharmacyName: string
  medicineId: string
  medicineName: string
  genericName: string
  batchCount: number
  totalQuantity: number
  totalReservedQuantity: number
  totalAvailableQuantity: number
  reorderLevel: number
  nearestExpirationDate?: string | null
  lastStockUpdatedAtUtc?: string | null
}

export interface ExpiringStockAlert {
  stockItemId: string
  pharmacyId: string
  pharmacyName: string
  medicineId: string
  medicineName: string
  genericName: string
  batchNumber: string
  expirationDate: string
  daysUntilExpiration: number
  quantity: number
  reservedQuantity: number
  availableQuantity: number
  retailPrice: number
  lastStockUpdatedAtUtc?: string | null
}

export interface RestockSuggestion {
  stockItemId: string
  pharmacyId: string
  pharmacyName: string
  medicineId: string
  medicineName: string
  genericName: string
  availableQuantity: number
  reorderLevel: number
  deficit: number
  suggestedOrderQuantity: number
  depotId: string
  depotName: string
  supplierAvailableQuantity: number
  minimumOrderQuantity: number
  estimatedDeliveryHours: number
  wholesalePrice: number
  estimatedWholesaleCost: number
}

export interface ManagedPharmacy {
  id: string
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
  isActive: boolean
  pharmacyChainId?: string | null
  pharmacyChainName?: string | null
  employeeCount: number
  activeStockItemCount: number
  activeReservationCount: number
  lastLocationVerifiedAtUtc?: string | null
  createdAtUtc: string
  updatedAtUtc?: string | null
}

export interface ManagedUser {
  id: string
  firstName: string
  lastName: string
  phoneNumber: string
  email?: string | null
  telegramUsername?: string | null
  telegramChatId?: string | null
  role: number
  isActive: boolean
  pharmacyId?: string | null
  pharmacyName?: string | null
  createdAtUtc: string
  updatedAtUtc?: string | null
}

export interface AuditLogEntry {
  id: string
  action: string
  entityName: string
  entityId?: string | null
  description: string
  metadataJson?: string | null
  userId?: string | null
  userFullName?: string | null
  pharmacyId?: string | null
  pharmacyName?: string | null
  createdAtUtc: string
}

export interface ManagedMedicineCategory {
  id: string
  name: string
  description?: string | null
  medicinesCount: number
}

export interface ManagedMedicine {
  id: string
  brandName: string
  genericName: string
  description?: string | null
  dosageForm: string
  strength: string
  manufacturer: string
  countryOfOrigin?: string | null
  barcode?: string | null
  requiresPrescription: boolean
  isActive: boolean
  categoryId?: string | null
  categoryName?: string | null
  stockBatchCount: number
  supplierOfferCount: number
}
