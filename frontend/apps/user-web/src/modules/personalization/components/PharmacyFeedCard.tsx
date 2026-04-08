import Link from 'next/link'
import type { ConsumerPharmacyFeedItem } from '@pharmago/types'
import { Button, Card, CardContent, CardHeader, CardTitle, StatusBadge } from '@pharmago/ui'
import { formatDateTime, formatMoney } from '../../../lib/format'

export interface PharmacyFeedCardProps {
  pharmacy: ConsumerPharmacyFeedItem
  onToggleFavorite: (input: { pharmacyId: string; isFavorite: boolean }) => void
  isPending?: boolean
}

export function PharmacyFeedCard({ pharmacy, onToggleFavorite, isPending = false }: PharmacyFeedCardProps) {
  return (
    <Card className="h-full">
      <CardHeader className="space-y-4">
        <div className="flex items-start justify-between gap-4">
          <div className="space-y-1">
            <CardTitle>{pharmacy.name}</CardTitle>
            <p className="text-sm text-slate-500">{pharmacy.chainName || pharmacy.city}</p>
          </div>
          <StatusBadge tone={pharmacy.supportsReservations ? 'success' : 'neutral'}>
            {pharmacy.supportsReservations ? 'Reservations on' : 'Reservations off'}
          </StatusBadge>
        </div>
        <p className="text-sm text-slate-600">{pharmacy.address}</p>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="grid gap-3 sm:grid-cols-2">
          <div className="rounded-2xl bg-slate-50 p-4">
            <p className="text-xs uppercase tracking-wide text-slate-400">Availability</p>
            <p className="mt-1 text-sm font-medium text-slate-950">{pharmacy.availableMedicineCount} medicines</p>
          </div>
          <div className="rounded-2xl bg-slate-50 p-4">
            <p className="text-xs uppercase tracking-wide text-slate-400">Starting price</p>
            <p className="mt-1 text-sm font-medium text-slate-950">{formatMoney(pharmacy.minAvailablePrice)}</p>
          </div>
        </div>

        <div className="rounded-2xl border border-slate-200 p-4 text-sm text-slate-600">
          <p>Last viewed: <span className="font-medium text-slate-950">{formatDateTime(pharmacy.lastViewedAtUtc)}</span></p>
          <p>Favorited at: <span className="font-medium text-slate-950">{formatDateTime(pharmacy.favoritedAtUtc)}</span></p>
        </div>

        <div className="flex flex-wrap gap-3">
          <Button asChild className="flex-1 min-w-[10rem]">
            <Link href={`/pharmacies/${pharmacy.pharmacyId}`}>Open pharmacy</Link>
          </Button>
          <Button
            type="button"
            variant={pharmacy.isFavorite ? 'outline' : 'secondary'}
            disabled={isPending}
            onClick={() => onToggleFavorite({ pharmacyId: pharmacy.pharmacyId, isFavorite: pharmacy.isFavorite })}
          >
            {pharmacy.isFavorite ? 'Remove favorite' : 'Add favorite'}
          </Button>
        </div>
      </CardContent>
    </Card>
  )
}
