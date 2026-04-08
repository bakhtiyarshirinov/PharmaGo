'use client'

import Link from 'next/link'
import { Button, EmptyState, PageHeader, ReservationCard } from '@pharmago/ui'
import { useMyReservations } from '../../../../modules/reservations/queries'

export default function ReservationsPage() {
  const reservations = useMyReservations()

  if (reservations.isLoading) {
    return (
      <div className="space-y-8">
        <PageHeader
          eyebrow="My reservations"
          title="Track every reservation from one place."
          description="See lifecycle progress, pickup readiness and expiry pressure without hunting through medicine detail pages."
        />
        <div className="grid gap-6">
          {Array.from({ length: 3 }).map((_, index) => (
            <div key={index} className="h-40 animate-pulse rounded-[2rem] bg-slate-100" />
          ))}
        </div>
      </div>
    )
  }

  if (reservations.isError) {
    return (
      <div className="space-y-8">
        <PageHeader
          eyebrow="My reservations"
          title="Track every reservation from one place."
          description="See lifecycle progress, pickup readiness and expiry pressure without hunting through medicine detail pages."
        />
        <EmptyState title="Unable to load reservations" description="Please refresh the page and try again." />
      </div>
    )
  }

  return (
    <div className="space-y-8">
      <PageHeader
        eyebrow="My reservations"
        title="Track every reservation from one place."
        description="See lifecycle progress, pickup readiness and expiry pressure without hunting through medicine detail pages."
      />

      {reservations.data?.length ? (
        <div className="grid gap-6">
          {reservations.data.map((reservation) => (
            <ReservationCard
              key={reservation.reservationId}
              reservation={reservation}
              actionSlot={(
                <Button asChild variant="outline" size="sm">
                  <Link href={`/app/reservations/${reservation.reservationId}`}>Open timeline</Link>
                </Button>
              )}
            />
          ))}
        </div>
      ) : (
        <EmptyState title="No reservations yet" description="Reserve a medicine from its detail page to start the flow." />
      )}
    </div>
  )
}
