import Link from 'next/link'
import type { PharmacyDetail } from '@pharmago/types'
import { Button, Card, CardContent, CardHeader, CardTitle, StatusBadge } from '@pharmago/ui'
import { formatDistance, formatMoney } from '../../../lib/format'

export interface PharmacySearchCardProps {
  pharmacy: PharmacyDetail
}

export function PharmacySearchCard({ pharmacy }: PharmacySearchCardProps) {
  return (
    <Card className="h-full">
      <CardHeader className="space-y-4">
        <div className="flex items-start justify-between gap-4">
          <div className="space-y-1">
            <CardTitle>{pharmacy.name}</CardTitle>
            <p className="text-sm text-slate-500">{pharmacy.chainName || pharmacy.city}</p>
          </div>
          <StatusBadge tone={pharmacy.isOpenNow ? 'success' : 'warning'}>
            {pharmacy.isOpenNow ? 'Open now' : 'Closed'}
          </StatusBadge>
        </div>
        <p className="text-sm text-slate-600">{pharmacy.address}</p>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="grid gap-3 sm:grid-cols-2">
          <div className="rounded-2xl bg-slate-50 p-4">
            <p className="text-xs uppercase tracking-wide text-slate-400">Discovery</p>
            <p className="mt-1 text-sm font-medium text-slate-900">{formatDistance(pharmacy.distanceKm)}</p>
          </div>
          <div className="rounded-2xl bg-slate-50 p-4">
            <p className="text-xs uppercase tracking-wide text-slate-400">Starting price</p>
            <p className="mt-1 text-sm font-medium text-slate-900">{formatMoney(pharmacy.minAvailablePrice)}</p>
          </div>
        </div>

        <div className="flex flex-wrap gap-2">
          <StatusBadge tone={pharmacy.supportsReservations ? 'info' : 'neutral'}>
            {pharmacy.supportsReservations ? 'Reservations on' : 'Reservations off'}
          </StatusBadge>
          <StatusBadge tone={pharmacy.hasDelivery ? 'success' : 'neutral'}>
            {pharmacy.hasDelivery ? 'Delivery' : 'Pickup only'}
          </StatusBadge>
        </div>

        <Button asChild className="w-full">
          <Link href={`/pharmacies/${pharmacy.pharmacyId}`}>Open pharmacy</Link>
        </Button>
      </CardContent>
    </Card>
  )
}
