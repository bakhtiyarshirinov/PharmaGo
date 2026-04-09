'use client'

import Link from 'next/link'
import { useParams, useRouter } from 'next/navigation'
import { usePermission } from '@pharmago/auth/client'
import { Button, Card, CardContent, CardHeader, CardTitle, EmptyState, PageHeader, StatusBadge } from '@pharmago/ui'
import { useMedicineAvailability, useMedicineDetail } from '../../../../modules/medicines/queries'
import { useCreateReservation } from '../../../../modules/reservations/hooks/useCreateReservation'
import { formatMoney } from '../../../../lib/format'

export default function MedicineDetailPage() {
  const params = useParams<{ medicineId: string }>()
  const router = useRouter()
  const permissions = usePermission()
  const medicine = useMedicineDetail(params.medicineId)
  const availability = useMedicineAvailability(params.medicineId)
  const createReservation = useCreateReservation()

  if (medicine.isLoading) {
    return (
      <div className="space-y-8">
        <div className="h-32 animate-pulse rounded-[2rem] bg-slate-100" />
        <div className="grid gap-6 lg:grid-cols-[1.2fr,0.8fr]">
          <div className="h-80 animate-pulse rounded-[2rem] bg-slate-100" />
          <div className="h-48 animate-pulse rounded-[2rem] bg-slate-100" />
        </div>
      </div>
    )
  }

  if (medicine.isError) {
    return (
      <EmptyState
        title="Не удалось загрузить карточку лекарства"
        description="Попробуйте обновить страницу или открыть карточку снова из списка лекарств."
      />
    )
  }

  if (!medicine.data) {
    return <EmptyState title="Medicine not found" description="The requested medicine detail is unavailable." />
  }

  return (
    <div className="space-y-8">
      <PageHeader
        eyebrow="Medicine detail"
        title={medicine.data.brandName}
        description={medicine.data.description || `${medicine.data.genericName} · ${medicine.data.dosageForm} · ${medicine.data.strength}`}
        actions={<StatusBadge tone={medicine.data.requiresPrescription ? 'warning' : 'success'}>{medicine.data.requiresPrescription ? 'Prescription' : 'OTC'}</StatusBadge>}
      />

      <div className="grid gap-6 lg:grid-cols-[1.2fr,0.8fr]">
        <Card>
          <CardHeader>
            <CardTitle>Availability by pharmacy</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            {availability.isLoading ? (
              Array.from({ length: 3 }).map((_, index) => (
                <div key={index} className="h-24 animate-pulse rounded-2xl bg-slate-100" />
              ))
            ) : availability.isError ? (
              <EmptyState
                title="Не удалось загрузить наличие"
                description="Карточка лекарства открылась, но список аптек сейчас недоступен."
              />
            ) : availability.data?.length ? (
              availability.data.map((item) => (
                <div key={`${item.pharmacyId}-${item.pharmacyName}`} className="flex flex-col gap-3 rounded-2xl border border-slate-200 p-4 md:flex-row md:items-center md:justify-between">
                  <div>
                    <Link href={`/pharmacies/${item.pharmacyId}`} className="font-medium text-slate-950 underline-offset-4 hover:underline">
                      {item.pharmacyName}
                    </Link>
                    <p className="text-sm text-slate-500">{item.address || item.city || 'Location available'}</p>
                  </div>
                  <div className="flex flex-wrap items-center gap-3">
                    <StatusBadge tone={item.supportsReservations ? 'success' : 'neutral'}>
                      {item.supportsReservations ? 'Reservable' : 'View only'}
                    </StatusBadge>
                    <span className="text-sm font-medium text-slate-900">{formatMoney(item.retailPrice)}</span>
                    <Button
                      onClick={async () => {
                        if (!permissions.canCreateReservation) {
                          router.push(`/auth/login?redirect=${encodeURIComponent(`/medicines/${params.medicineId}`)}`)
                          return
                        }

                        const reservation = await createReservation.mutateAsync({
                          pharmacyId: item.pharmacyId,
                          reserveForHours: 2,
                          items: [{ medicineId: params.medicineId, quantity: 1 }],
                        })

                        router.push(`/app/reservations/${reservation.reservationId}`)
                      }}
                      disabled={!item.supportsReservations}
                    >
                      Reserve here
                    </Button>
                  </div>
                </div>
              ))
            ) : (
              <EmptyState
                title="Нет доступных аптек"
                description="Для этого лекарства сейчас не найдено активного наличия в витрине аптек."
              />
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Purchase posture</CardTitle>
          </CardHeader>
          <CardContent className="space-y-3 text-sm text-slate-600">
            <p>Pharmacies carrying this medicine: <span className="font-semibold text-slate-950">{medicine.data.pharmacyCount}</span></p>
            <p>Total visible quantity: <span className="font-semibold text-slate-950">{medicine.data.totalAvailableQuantity}</span></p>
            <p>Lowest visible price: <span className="font-semibold text-slate-950">{formatMoney(medicine.data.minRetailPrice)}</span></p>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
