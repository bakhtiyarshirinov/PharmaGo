'use client'

import { useParams, useRouter } from 'next/navigation'
import { usePermission } from '@pharmago/auth/client'
import { Button, Card, CardContent, CardHeader, CardTitle, EmptyState, PageHeader, StatusBadge } from '@pharmago/ui'
import { PharmacyCatalogList } from '../../../../modules/pharmacies/components/PharmacyCatalogList'
import { usePharmacyDetail, usePharmacyMedicines } from '../../../../modules/pharmacies/queries'
import { useCreateReservation } from '../../../../modules/reservations/hooks/useCreateReservation'
import { parseOpeningHours } from '../../../../lib/domain'
import { formatDateTime, formatDistance, formatMoney } from '../../../../lib/format'

export default function PharmacyDetailPage() {
  const params = useParams<{ pharmacyId: string }>()
  const router = useRouter()
  const permissions = usePermission()
  const pharmacy = usePharmacyDetail(params.pharmacyId)
  const catalog = usePharmacyMedicines(params.pharmacyId)
  const createReservation = useCreateReservation()

  const schedule = parseOpeningHours(pharmacy.data?.openingHoursJson)

  if (!pharmacy.data) {
    return <EmptyState title="Pharmacy not found" description="The requested pharmacy profile is unavailable." />
  }

  return (
    <div className="space-y-8">
      <PageHeader
        eyebrow="Pharmacy detail"
        title={pharmacy.data.name}
        description={pharmacy.data.address}
        actions={
          <StatusBadge tone={pharmacy.data.isOpenNow ? 'success' : 'warning'}>
            {pharmacy.data.isOpenNow ? 'Open now' : 'Closed'}
          </StatusBadge>
        }
      />

      <div className="grid gap-6 xl:grid-cols-[1.25fr,0.75fr]">
        <div className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>Store profile</CardTitle>
            </CardHeader>
            <CardContent className="grid gap-4 md:grid-cols-2">
              <div className="rounded-2xl bg-slate-50 p-4">
                <p className="text-xs uppercase tracking-wide text-slate-400">Chain</p>
                <p className="mt-1 text-sm font-medium text-slate-950">{pharmacy.data.chainName || 'Independent pharmacy'}</p>
              </div>
              <div className="rounded-2xl bg-slate-50 p-4">
                <p className="text-xs uppercase tracking-wide text-slate-400">Discovery</p>
                <p className="mt-1 text-sm font-medium text-slate-950">{formatDistance(pharmacy.data.distanceKm)}</p>
              </div>
              <div className="rounded-2xl bg-slate-50 p-4">
                <p className="text-xs uppercase tracking-wide text-slate-400">Available medicines</p>
                <p className="mt-1 text-sm font-medium text-slate-950">{pharmacy.data.availableMedicineCount}</p>
              </div>
              <div className="rounded-2xl bg-slate-50 p-4">
                <p className="text-xs uppercase tracking-wide text-slate-400">Starting visible price</p>
                <p className="mt-1 text-sm font-medium text-slate-950">{formatMoney(pharmacy.data.minAvailablePrice)}</p>
              </div>
            </CardContent>
          </Card>

          {catalog.data?.items?.length ? (
            <PharmacyCatalogList
              items={catalog.data.items}
              reserveDisabled={createReservation.isPending}
              onReserve={async ({ medicineId, quantity }) => {
                if (!permissions.canCreateReservation) {
                  router.push(`/auth/login?redirect=${encodeURIComponent(`/pharmacies/${params.pharmacyId}`)}`)
                  return
                }

                const reservation = await createReservation.mutateAsync({
                  pharmacyId: params.pharmacyId,
                  reserveForHours: 2,
                  items: [{ medicineId, quantity }],
                })

                router.push(`/app/reservations/${reservation.reservationId}`)
              }}
            />
          ) : (
            <EmptyState title="No visible catalog items" description="This pharmacy currently has no published catalog entries." />
          )}
        </div>

        <div className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>Reservation policy</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3 text-sm text-slate-600">
              <p>Reservations: <span className="font-semibold text-slate-950">{pharmacy.data.supportsReservations ? 'Enabled' : 'Disabled'}</span></p>
              <p>Delivery: <span className="font-semibold text-slate-950">{pharmacy.data.hasDelivery ? 'Available' : 'Not available'}</span></p>
              <p>Phone: <span className="font-semibold text-slate-950">{pharmacy.data.phoneNumber || 'Not published'}</span></p>
              <p>Pickup note: <span className="font-semibold text-slate-950">If closed, pickup starts after the next opening window.</span></p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Opening hours</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              {pharmacy.data.isOpen24Hours ? (
                <StatusBadge tone="success">Open 24/7</StatusBadge>
              ) : schedule.length ? (
                <div className="space-y-2">
                  {schedule.map((entry) => (
                    <div key={`${entry.day}-${entry.open}-${entry.close}`} className="flex items-center justify-between rounded-2xl bg-slate-50 px-4 py-3 text-sm">
                      <span className="font-medium text-slate-900">{entry.day}</span>
                      <span className="text-slate-500">{entry.open} - {entry.close}</span>
                    </div>
                  ))}
                </div>
              ) : (
                <EmptyState title="Schedule unavailable" description="The pharmacy has not published a validated weekly schedule yet." />
              )}

              <p className="text-xs uppercase tracking-wide text-slate-400">
                Schedule rendered in local Azerbaijan time. Updated when pharmacy admin changes hours.
              </p>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  )
}
