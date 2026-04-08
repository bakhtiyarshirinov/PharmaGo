'use client'

import { useMemo } from 'react'
import { useRouter, useSearchParams } from 'next/navigation'
import { Button, EmptyState, Input, PageHeader } from '@pharmago/ui'
import { PharmacySearchCard } from '../../../modules/pharmacies/components/PharmacySearchCard'
import { usePharmaciesSearch, usePopularPharmacies } from '../../../modules/pharmacies/queries'
import { pharmacySearchSchema } from '../../../modules/pharmacies/schemas'

export default function PharmaciesPage() {
  const router = useRouter()
  const searchParams = useSearchParams()

  const filters = useMemo(
    () =>
      pharmacySearchSchema.parse({
        query: searchParams.get('q') ?? '',
        page: searchParams.get('page') ?? '1',
        pageSize: searchParams.get('pageSize') ?? '9',
      }),
    [searchParams],
  )

  const search = usePharmaciesSearch({
    query: filters.query,
    page: filters.page,
    pageSize: filters.pageSize,
  })
  const popular = usePopularPharmacies()

  const list = filters.query ? search.data?.items ?? [] : popular.data ?? []

  return (
    <div className="space-y-8">
      <PageHeader
        eyebrow="Pharmacy search"
        title="Find the right pharmacy before you commit to a reservation."
        description="This discovery surface is optimized for proximity, services, stock breadth and whether a pharmacy can actually handle reservations."
      />

      <form
        className="flex flex-col gap-3 rounded-[2rem] border border-slate-200 bg-white p-5 md:flex-row"
        onSubmit={(event) => {
          event.preventDefault()
          const formData = new FormData(event.currentTarget)
          const query = String(formData.get('query') ?? '')
          const next = new URLSearchParams()
          if (query.trim()) {
            next.set('q', query.trim())
          }
          next.set('page', '1')
          next.set('pageSize', String(filters.pageSize))
          router.push(`/pharmacies?${next.toString()}`)
        }}
      >
        <Input name="query" defaultValue={filters.query} placeholder="Search pharmacies by name, chain or district" />
        <Button type="submit" variant="secondary">Search</Button>
      </form>

      {list.length ? (
        <div className="grid gap-6 md:grid-cols-2 xl:grid-cols-3">
          {list.map((pharmacy) => (
            <PharmacySearchCard key={pharmacy.pharmacyId} pharmacy={pharmacy} />
          ))}
        </div>
      ) : (
        <EmptyState
          title={filters.query ? 'No pharmacies found' : 'No popular pharmacies yet'}
          description={filters.query ? 'Try another pharmacy name, chain or district.' : 'Popular pharmacy recommendations will appear here.'}
        />
      )}
    </div>
  )
}
