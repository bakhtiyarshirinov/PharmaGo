'use client'

import Link from 'next/link'
import { Button, EmptyState, PageHeader, ReservationCard } from '@pharmago/ui'
import { useMyReservations } from '../../../../modules/reservations/queries'

export default function ReservationsPage() {
  const reservations = useMyReservations()

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
