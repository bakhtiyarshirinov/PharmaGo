import type { PharmacyMedicineItem } from '@pharmago/types'
import { Button, Card, CardContent, CardHeader, CardTitle, StatusBadge } from '@pharmago/ui'
import { formatMoney } from '../../../lib/format'

export interface PharmacyCatalogListProps {
  items: PharmacyMedicineItem[]
  onReserve: (input: { medicineId: string; quantity: number }) => void
  reserveDisabled?: boolean
}

export function PharmacyCatalogList({ items, onReserve, reserveDisabled = false }: PharmacyCatalogListProps) {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Available medicines</CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        {items.map((item) => (
          <div
            key={item.medicineId}
            className="flex flex-col gap-4 rounded-2xl border border-slate-200 p-4 lg:flex-row lg:items-center lg:justify-between"
          >
            <div className="space-y-1">
              <h3 className="font-medium text-slate-950">{item.brandName}</h3>
              <p className="text-sm text-slate-500">
                {item.genericName} · {item.dosageForm || 'Form N/A'} · {item.strength || 'Strength N/A'}
              </p>
            </div>

            <div className="flex flex-wrap items-center gap-3">
              <StatusBadge tone={item.isReservable ? 'success' : 'neutral'}>
                {item.isReservable ? 'Reservable' : 'View only'}
              </StatusBadge>
              <StatusBadge tone={item.availableQuantity > 0 ? 'info' : 'danger'}>
                {item.availableQuantity} units
              </StatusBadge>
              <span className="text-sm font-medium text-slate-900">{formatMoney(item.retailPrice)}</span>
              <Button
                type="button"
                size="sm"
                onClick={() => onReserve({ medicineId: item.medicineId, quantity: 1 })}
                disabled={reserveDisabled || !item.isReservable || item.availableQuantity < 1}
              >
                Reserve
              </Button>
            </div>
          </div>
        ))}
      </CardContent>
    </Card>
  )
}
