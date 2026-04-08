import { EmptyState, PageHeader } from '@pharmago/ui'

export default function PharmacistReservationsPage() {
  return (
    <div className="space-y-8">
      <PageHeader
        eyebrow="Reservation queue"
        title="Confirm, prepare and complete reservations fast."
        description="This screen should be optimized for queue handling, not for browsing."
      />
      <EmptyState title="Reservation queue scaffold" description="Wire active reservations, detail drawer and action buttons here." />
    </div>
  )
}

