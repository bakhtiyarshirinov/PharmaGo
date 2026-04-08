import Link from 'next/link'
import type { MedicineSearchItem } from '@pharmago/types'
import { Button, Card, CardContent, CardHeader, CardTitle, StatusBadge } from '@pharmago/ui'

export function MedicineCard({ medicine }: { medicine: MedicineSearchItem }) {
  return (
    <Card className="h-full">
      <CardHeader>
        <div className="flex items-start justify-between gap-4">
          <div>
            <CardTitle>{medicine.brandName}</CardTitle>
            <p className="mt-1 text-sm text-slate-500">
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
            <p className="text-xs uppercase tracking-wide text-slate-400">Pharmacies</p>
            <p className="mt-1 text-lg font-semibold text-slate-950">{medicine.pharmacyCount}</p>
          </div>
          <div className="rounded-2xl bg-slate-50 p-4">
            <p className="text-xs uppercase tracking-wide text-slate-400">Starting price</p>
            <p className="mt-1 text-lg font-semibold text-slate-950">{medicine.minRetailPrice ?? 'N/A'}</p>
          </div>
        </div>
        <Button asChild className="w-full">
          <Link href={`/medicines/${medicine.medicineId}`}>Open medicine</Link>
        </Button>
      </CardContent>
    </Card>
  )
}

