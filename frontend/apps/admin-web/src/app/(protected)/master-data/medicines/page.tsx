import { EmptyState, PageHeader } from '@pharmago/ui'

export default function AdminMedicinesPage() {
  return (
    <div className="space-y-8">
      <PageHeader eyebrow="Master data" title="Medicines catalog admin" description="This is the main master-data screen: table, filters, create/edit forms and validation feedback." />
      <EmptyState title="Medicines admin scaffold" description="Wire the admin master-data medicines endpoints here." />
    </div>
  )
}

