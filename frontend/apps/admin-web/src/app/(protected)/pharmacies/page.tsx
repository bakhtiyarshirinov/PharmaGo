import { EmptyState, PageHeader } from '@pharmago/ui'

export default function AdminPharmaciesPage() {
  return (
    <div className="space-y-8">
      <PageHeader eyebrow="Pharmacies" title="Pharmacy management" description="Use a server-paginated table with a wide drawer for editing pharmacy profile and schedule." />
      <EmptyState title="Pharmacies admin scaffold" description="Connect admin pharmacy CRUD and schedule management here." />
    </div>
  )
}

