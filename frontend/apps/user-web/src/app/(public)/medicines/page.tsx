'use client'

import { useEffect, useMemo, useState } from 'react'
import { useRouter, useSearchParams } from 'next/navigation'
import { EmptyState, PageHeader } from '@pharmago/ui'
import { QueryStateCard } from '../../../components/QueryStateCard'
import { SearchAutocomplete } from '../../../components/SearchAutocomplete'
import { SearchResultsSkeleton } from '../../../components/SearchResultsSkeleton'
import { useDebouncedValue } from '../../../hooks/useDebouncedValue'
import { MedicineCard } from '../../../modules/medicines/components/MedicineCard'
import { useMedicineSuggestions, useMedicinesSearch, usePopularMedicines } from '../../../modules/medicines/queries'

export default function MedicinesPage() {
  const router = useRouter()
  const searchParams = useSearchParams()
  const initialQuery = searchParams.get('query') ?? ''
  const [query, setQuery] = useState(initialQuery)
  const debouncedQuery = useDebouncedValue(query, 350)
  const normalizedQuery = debouncedQuery.trim()

  useEffect(() => {
    const next = new URLSearchParams(searchParams.toString())

    if (normalizedQuery) {
      next.set('query', normalizedQuery)
    } else {
      next.delete('query')
    }

    const nextUrl = next.toString() ? `/medicines?${next.toString()}` : '/medicines'
    router.replace(nextUrl, { scroll: false })
  }, [normalizedQuery, router, searchParams])

  const search = useMedicinesSearch(normalizedQuery)
  const suggestions = useMedicineSuggestions(query)
  const popular = usePopularMedicines()

  const medicines = normalizedQuery ? search.data ?? [] : popular.data ?? []
  const sectionTitle = normalizedQuery ? `Results for "${normalizedQuery}"` : 'Popular medicines'
  const sectionDescription = normalizedQuery
    ? 'Search results update as you type, while suggestions help you pivot without losing context.'
    : 'Use the search box to jump into a medicine-first reservation flow.'

  const retry = useMemo(() => {
    if (normalizedQuery) {
      return () => search.refetch()
    }

    return () => popular.refetch()
  }, [normalizedQuery, popular, search])

  return (
    <div className="space-y-8">
      <PageHeader
        eyebrow="Medicines search"
        title="Discover medicines with availability context, not just names."
        description="Search results should immediately communicate stock breadth, price edge and whether the medicine is prescription-only."
      />

      <form
        className="rounded-[2rem] border border-slate-200 bg-white p-5"
        onSubmit={(event) => {
          event.preventDefault()
          const committed = query.trim()
          setQuery(committed)
          const next = committed ? `/medicines?query=${encodeURIComponent(committed)}` : '/medicines'
          router.push(next)
        }}
      >
        <SearchAutocomplete
          value={query}
          onValueChange={setQuery}
          onSubmit={(value) => {
            const committed = value.trim()
            const next = committed ? `/medicines?query=${encodeURIComponent(committed)}` : '/medicines'
            router.push(next)
          }}
          placeholder="Search medicines by brand, generic or barcode"
          suggestions={suggestions.data ?? []}
          isLoading={suggestions.isFetching}
          isError={suggestions.isError}
          onRetry={() => suggestions.refetch()}
          getKey={(item) => item.medicineId}
          getTitle={(item) => item.brandName}
          getDescription={(item) => item.genericName}
          onSelect={(item) => {
            setQuery(item.brandName)
            router.push(`/medicines/${item.medicineId}`)
          }}
          emptyTitle="No medicine suggestions found"
          loadingTitle="Searching medicine suggestions..."
          errorTitle="Medicine suggestions are temporarily unavailable"
        />
      </form>

      <div className="space-y-3">
        <h2 className="text-xl font-semibold text-slate-950">{sectionTitle}</h2>
        <p className="text-sm text-slate-500">{sectionDescription}</p>
      </div>

      {(normalizedQuery ? search.isPending : popular.isPending) ? (
        <SearchResultsSkeleton />
      ) : normalizedQuery && search.isError ? (
        <QueryStateCard
          title="Medicine search failed"
          description="The search service could not return medicine results. Try the same query again."
          onAction={retry}
        />
      ) : !normalizedQuery && popular.isError ? (
        <QueryStateCard
          title="Popular medicines are unavailable"
          description="We could not load the default medicine discovery rail. Retry to fetch the latest popular items."
          onAction={retry}
        />
      ) : medicines.length ? (
        <div className="grid gap-6 md:grid-cols-2 xl:grid-cols-3">
          {medicines.map((medicine) => (
            <MedicineCard key={medicine.medicineId} medicine={medicine} />
          ))}
        </div>
      ) : (
        <EmptyState
          title={normalizedQuery ? 'No medicines found' : 'No medicines to explore yet'}
          description={
            normalizedQuery
              ? 'Try a generic name, barcode or another brand.'
              : 'Popular medicines will appear here once the discovery feed has data.'
          }
        />
      )}
    </div>
  )
}
