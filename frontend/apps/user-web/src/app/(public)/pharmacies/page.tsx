'use client'

import { useEffect, useMemo, useState } from 'react'
import { useRouter, useSearchParams } from 'next/navigation'
import { EmptyState, PageHeader } from '@pharmago/ui'
import { QueryStateCard } from '../../../components/QueryStateCard'
import { SearchAutocomplete } from '../../../components/SearchAutocomplete'
import { SearchResultsSkeleton } from '../../../components/SearchResultsSkeleton'
import { useDebouncedValue } from '../../../hooks/useDebouncedValue'
import { PharmacySearchCard } from '../../../modules/pharmacies/components/PharmacySearchCard'
import { usePharmaciesSearch, usePharmacySuggestions, usePopularPharmacies } from '../../../modules/pharmacies/queries'
import { pharmacySearchSchema } from '../../../modules/pharmacies/schemas'

export default function PharmaciesPage() {
  const router = useRouter()
  const searchParams = useSearchParams()
  const [query, setQuery] = useState(searchParams.get('q') ?? '')
  const debouncedQuery = useDebouncedValue(query, 350)

  const filters = useMemo(
    () =>
      pharmacySearchSchema.parse({
        query: debouncedQuery,
        page: searchParams.get('page') ?? '1',
        pageSize: searchParams.get('pageSize') ?? '9',
      }),
    [debouncedQuery, searchParams],
  )

  useEffect(() => {
    const next = new URLSearchParams(searchParams.toString())

    if (filters.query) {
      next.set('q', filters.query)
    } else {
      next.delete('q')
    }

    next.set('page', '1')
    next.set('pageSize', String(filters.pageSize))

    const nextUrl = next.toString() ? `/pharmacies?${next.toString()}` : '/pharmacies'
    router.replace(nextUrl, { scroll: false })
  }, [filters.pageSize, filters.query, router, searchParams])

  const search = usePharmaciesSearch({
    query: filters.query,
    page: filters.page,
    pageSize: filters.pageSize,
  })
  const popular = usePopularPharmacies()
  const suggestions = usePharmacySuggestions(query)

  const list = filters.query ? search.data?.items ?? [] : popular.data ?? []
  const sectionTitle = filters.query ? `Pharmacies matching "${filters.query}"` : 'Popular pharmacies'
  const sectionDescription = filters.query
    ? 'Discovery is shaped around proximity, reservation support and visible price floor.'
    : 'Use search or suggestions to jump directly into a pharmacy-first reservation flow.'
  const retry = useMemo(() => {
    if (filters.query) {
      return () => search.refetch()
    }

    return () => popular.refetch()
  }, [filters.query, popular, search])

  return (
    <div className="space-y-8">
      <PageHeader
        eyebrow="Pharmacy search"
        title="Find the right pharmacy before you commit to a reservation."
        description="This discovery surface is optimized for proximity, services, stock breadth and whether a pharmacy can actually handle reservations."
      />

      <form
        className="rounded-[2rem] border border-slate-200 bg-white p-5"
        onSubmit={(event) => {
          event.preventDefault()
          const committed = query.trim()
          const next = new URLSearchParams()
          if (committed) {
            next.set('q', committed)
          }
          next.set('page', '1')
          next.set('pageSize', String(filters.pageSize))
          router.push(`/pharmacies?${next.toString()}`)
        }}
      >
        <SearchAutocomplete
          value={query}
          onValueChange={setQuery}
          onSubmit={(value) => {
            const committed = value.trim()
            const next = committed ? `/pharmacies?q=${encodeURIComponent(committed)}` : '/pharmacies'
            router.push(next)
          }}
          placeholder="Search pharmacies by name, chain or district"
          suggestions={suggestions.data ?? []}
          isLoading={suggestions.isFetching}
          isError={suggestions.isError}
          onRetry={() => suggestions.refetch()}
          getKey={(item) => item.pharmacyId}
          getTitle={(item) => item.name}
          getDescription={(item) => item.city || 'Pharmacy suggestion'}
          onSelect={(item) => {
            setQuery(item.name)
            router.push(`/pharmacies/${item.pharmacyId}`)
          }}
          emptyTitle="No pharmacy suggestions found"
          loadingTitle="Searching pharmacy suggestions..."
          errorTitle="Pharmacy suggestions are temporarily unavailable"
        />
      </form>

      <div className="space-y-3">
        <h2 className="text-xl font-semibold text-slate-950">{sectionTitle}</h2>
        <p className="text-sm text-slate-500">{sectionDescription}</p>
      </div>

      {(filters.query ? search.isPending : popular.isPending) ? (
        <SearchResultsSkeleton />
      ) : filters.query && search.isError ? (
        <QueryStateCard
          title="Pharmacy search failed"
          description="We could not load pharmacy results for this search. Retry to fetch the latest discovery set."
          onAction={retry}
        />
      ) : !filters.query && popular.isError ? (
        <QueryStateCard
          title="Popular pharmacies are unavailable"
          description="Default pharmacy discovery could not be loaded. Retry to refresh the popular list."
          onAction={retry}
        />
      ) : list.length ? (
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
