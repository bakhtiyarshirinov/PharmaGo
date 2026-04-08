'use client'

import Link from 'next/link'
import { useParams } from 'next/navigation'
import { Button, Card, CardContent, CardHeader, CardTitle, EmptyState, PageHeader, StatusBadge } from '@pharmago/ui'
import { useCancelReservation } from '../../../../../modules/reservations/hooks/useCancelReservation'
import { useReservationDetail, useReservationTimeline } from '../../../../../modules/reservations/queries'
import { ReservationTimeline } from '../../../../../modules/reservations/components/ReservationTimeline'
import { canCancelReservation, getReservationStatusLabel, getReservationStatusTone } from '../../../../../lib/domain'
import { formatDateTime, formatMoney } from '../../../../../lib/format'

export default function ReservationDetailPage() {
  const params = useParams<{ reservationId: string }>()
  const reservation = useReservationDetail(params.reservationId)
  const timeline = useReservationTimeline(params.reservationId)
  const cancelReservation = useCancelReservation()

  if (reservation.isLoading) {
    return (
      <div className="space-y-8">
        <div className="h-36 animate-pulse rounded-[2rem] bg-slate-100" />
        <div className="grid gap-6 xl:grid-cols-[1.1fr,0.9fr]">
          <div className="h-80 animate-pulse rounded-[2rem] bg-slate-100" />
          <div className="h-80 animate-pulse rounded-[2rem] bg-slate-100" />
        </div>
      </div>
    )
  }

  if (reservation.isError || !reservation.data) {
    return <EmptyState title="Reservation not found" description="The requested reservation could not be loaded." />
  }

  const canCancel = canCancelReservation(reservation.data)

  return (
    <div className="space-y-8">
      <PageHeader
        eyebrow="Reservation detail"
        title={reservation.data.reservationNumber}
        description={`Pickup from ${reservation.data.pharmacyName}`}
        actions={
          <StatusBadge tone={getReservationStatusTone(reservation.data.status)}>
            {getReservationStatusLabel(reservation.data.status)}
          </StatusBadge>
        }
      />

      <div className="grid gap-6 xl:grid-cols-[1.1fr,0.9fr]">
        <div className="space-y-6">
          <Card>
            <CardHeader className="flex flex-row items-start justify-between gap-4">
              <div className="space-y-1">
                <CardTitle>Reservation summary</CardTitle>
                <p className="text-sm text-slate-500">Customer-facing state for the reservation lifecycle.</p>
              </div>
              {canCancel ? (
                <Button
                  variant="destructive"
                  size="sm"
                  disabled={cancelReservation.isPending}
                  onClick={() => cancelReservation.mutate(reservation.data!.reservationId)}
                >
                  Cancel reservation
                </Button>
              ) : null}
            </CardHeader>
            <CardContent className="grid gap-4 md:grid-cols-2">
              <div className="rounded-2xl bg-slate-50 p-4">
                <p className="text-xs uppercase tracking-wide text-slate-400">Reserved until</p>
                <p className="mt-1 text-sm font-medium text-slate-950">{formatDateTime(reservation.data.reservedUntilUtc)}</p>
              </div>
              <div className="rounded-2xl bg-slate-50 p-4">
                <p className="text-xs uppercase tracking-wide text-slate-400">Pickup available from</p>
                <p className="mt-1 text-sm font-medium text-slate-950">{formatDateTime(reservation.data.pickupAvailableFromUtc)}</p>
              </div>
              <div className="rounded-2xl bg-slate-50 p-4">
                <p className="text-xs uppercase tracking-wide text-slate-400">Total amount</p>
                <p className="mt-1 text-sm font-medium text-slate-950">{formatMoney(reservation.data.totalAmount)}</p>
              </div>
              <div className="rounded-2xl bg-slate-50 p-4">
                <p className="text-xs uppercase tracking-wide text-slate-400">Pharmacy</p>
                <Button asChild variant="ghost" size="sm" className="mt-1 h-auto px-0 text-sm font-medium">
                  <Link href={`/pharmacies/${reservation.data.pharmacyId}`}>{reservation.data.pharmacyName}</Link>
                </Button>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Reserved items</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              {reservation.data.items.map((item) => (
                <div key={`${item.medicineId}-${item.medicineName}`} className="flex items-center justify-between rounded-2xl border border-slate-200 p-4">
                  <div>
                    <p className="font-medium text-slate-950">{item.medicineName}</p>
                    <p className="text-sm text-slate-500">{item.quantity} units reserved</p>
                  </div>
                  <p className="text-sm font-medium text-slate-950">{formatMoney(item.unitPrice * item.quantity)}</p>
                </div>
              ))}
            </CardContent>
          </Card>
        </div>

        {timeline.isLoading ? (
          <Card>
            <CardHeader>
              <CardTitle>Reservation timeline</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              {Array.from({ length: 4 }).map((_, index) => (
                <div key={index} className="h-24 animate-pulse rounded-2xl bg-slate-100" />
              ))}
            </CardContent>
          </Card>
        ) : timeline.isError ? (
          <EmptyState title="Timeline unavailable" description="Reservation events could not be loaded right now." />
        ) : (
          <ReservationTimeline events={timeline.data ?? []} />
        )}
      </div>
    </div>
  )
}
