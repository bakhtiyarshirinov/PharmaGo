import { Navigate, Route, Routes } from 'react-router-dom'
import { AppShell } from './layout/AppShell'
import { MedicinesPage } from '../pages/medicines/MedicinesPage'
import { OverviewPage } from '../pages/overview/OverviewPage'
import { PharmaciesPage } from '../pages/pharmacies/PharmaciesPage'
import { ReservationsPage } from '../pages/reservations/ReservationsPage'
import { StaffPage } from '../pages/staff/StaffPage'
import { useSessionStore } from '../features/auth/model/useSessionStore'

export function AppRouter() {
  const session = useSessionStore((state) => state.session)
  const isStaff = session?.user?.role === 2 || session?.user?.role === 3

  return (
    <AppShell>
      <Routes>
        <Route path="/" element={<OverviewPage />} />
        <Route path="/medicines" element={<MedicinesPage />} />
        <Route path="/medicines/:medicineId" element={<MedicinesPage />} />
        <Route path="/pharmacies" element={<PharmaciesPage />} />
        <Route path="/pharmacies/:pharmacyId" element={<PharmaciesPage />} />
        <Route path="/reservations" element={<ReservationsPage />} />
        <Route path="/staff" element={isStaff ? <StaffPage /> : <Navigate to="/" replace />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </AppShell>
  )
}
