import { PageHeader, StockAdjustmentDrawer } from '@pharmago/ui'
import type { StockItem } from '@pharmago/types'

const exampleItem: StockItem = {
  stockItemId: 'stock-1',
  medicineId: 'medicine-1',
  medicineName: 'Panadol',
  pharmacyId: 'pharmacy-1',
  pharmacyName: 'PharmaGo Central',
  batchNumber: 'BATCH-101',
  quantity: 20,
  reservedQuantity: 2,
  availableQuantity: 18,
  retailPrice: 4.5,
  expirationDate: new Date().toISOString(),
  isReservable: true,
}

export default function InventoryPage() {
  return (
    <div className="space-y-8">
      <PageHeader
        eyebrow="Inventory operations"
        title="Shift-safe stock management"
        description="Adjust, receive and write off stock with reasoned, auditable actions."
        actions={<StockAdjustmentDrawer stockItem={exampleItem} onSubmit={async () => undefined} />}
      />
    </div>
  )
}

