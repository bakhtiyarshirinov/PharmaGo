'use client'

import { useState } from 'react'
import { Button, EmptyState, Input, PageHeader } from '@pharmago/ui'
import { MedicineCard } from '../../../modules/medicines/components/MedicineCard'
import { useMedicinesSearch } from '../../../modules/medicines/hooks/useMedicinesSearch'

export default function MedicinesPage() {
  const [query, setQuery] = useState('Panadol')
  const search = useMedicinesSearch(query)

  return (
    <div className="space-y-8">
      <PageHeader
        eyebrow="Medicines search"
        title="Discover medicines with availability context, not just names."
        description="Search results should immediately communicate stock breadth, price edge and whether the medicine is prescription-only."
      />

      <div className="flex flex-col gap-3 rounded-[2rem] border border-slate-200 bg-white p-5 md:flex-row">
        <Input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Search medicines by brand, generic or barcode" />
        <Button variant="secondary">Search</Button>
      </div>

      {search.data?.length ? (
        <div className="grid gap-6 md:grid-cols-2 xl:grid-cols-3">
          {search.data.map((medicine) => (
            <MedicineCard key={medicine.medicineId} medicine={medicine} />
          ))}
        </div>
      ) : (
        <EmptyState title="No medicines found" description="Try a generic name, barcode or another brand." />
      )}
    </div>
  )
}

