import Link from 'next/link'
import type { ConsumerMedicineFeedItem } from '@pharmago/types'
import { Button, Card, CardContent, CardHeader, CardTitle, StatusBadge } from '@pharmago/ui'
import { formatDateTime, formatMoney } from '../../../lib/format'

export interface MedicineFeedCardProps {
  medicine: ConsumerMedicineFeedItem
  onToggleFavorite: (input: { medicineId: string; isFavorite: boolean }) => void
  isPending?: boolean
}

export function MedicineFeedCard({ medicine, onToggleFavorite, isPending = false }: MedicineFeedCardProps) {
  return (
    <Card className="h-full">
      <CardHeader className="space-y-4">
        <div className="flex items-start justify-between gap-4">
          <div className="space-y-1">
            <CardTitle>{medicine.brandName}</CardTitle>
            <p className="text-sm text-slate-500">
              {medicine.genericName} · {medicine.dosageForm} · {medicine.strength}
            </p>
          </div>
          <StatusBadge tone={medicine.requiresPrescription ? 'warning' : 'success'}>
            {medicine.requiresPrescription ? 'Rx' : 'OTC'}
          </StatusBadge>
        </div>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="grid gap-3 sm:grid-cols-2">
          <div className="rounded-2xl bg-slate-50 p-4">
            <p className="text-xs uppercase tracking-wide text-slate-400">Availability</p>
            <p className="mt-1 text-sm font-medium text-slate-950">{medicine.hasAvailability ? `${medicine.pharmacyCount} pharmacies` : 'No visible stock'}</p>
          </div>
          <div className="rounded-2xl bg-slate-50 p-4">
            <p className="text-xs uppercase tracking-wide text-slate-400">Starting price</p>
            <p className="mt-1 text-sm font-medium text-slate-950">{formatMoney(medicine.minRetailPrice)}</p>
          </div>
        </div>

        <div className="rounded-2xl border border-slate-200 p-4 text-sm text-slate-600">
          <p>Last viewed: <span className="font-medium text-slate-950">{formatDateTime(medicine.lastViewedAtUtc)}</span></p>
          <p>Favorited at: <span className="font-medium text-slate-950">{formatDateTime(medicine.favoritedAtUtc)}</span></p>
        </div>

        <div className="flex flex-wrap gap-3">
          <Button asChild className="flex-1 min-w-[10rem]">
            <Link href={`/medicines/${medicine.medicineId}`}>Open medicine</Link>
          </Button>
          <Button
            type="button"
            variant={medicine.isFavorite ? 'outline' : 'secondary'}
            disabled={isPending}
            onClick={() => onToggleFavorite({ medicineId: medicine.medicineId, isFavorite: medicine.isFavorite })}
          >
            {medicine.isFavorite ? 'Remove favorite' : 'Add favorite'}
          </Button>
        </div>
      </CardContent>
    </Card>
  )
}
