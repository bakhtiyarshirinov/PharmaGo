'use client'

import Link from 'next/link'
import { Button, EmptyState, PageHeader } from '@pharmago/ui'
import { QueryStateCard } from '../../../../../components/QueryStateCard'
import { PharmacyFeedCard } from '../../../../../modules/personalization/components/PharmacyFeedCard'
import { PersonalizationSkeleton } from '../../../../../modules/personalization/components/PersonalizationSkeleton'
import { useTogglePharmacyFavorite } from '../../../../../modules/personalization/pharmacies/mutations'
import { useRecentPharmacies } from '../../../../../modules/personalization/pharmacies/queries'

export default function RecentPharmaciesPage() {
  const recent = useRecentPharmacies()
  const toggleFavorite = useTogglePharmacyFavorite()

  return (
    <div className="space-y-8">
      <PageHeader
        eyebrow="Recent pharmacies"
        title="Pharmacies you opened recently"
        description="This gives users a quick return path into pharmacy-first reservation journeys."
      />

      {recent.isPending ? (
        <PersonalizationSkeleton />
      ) : recent.isError ? (
        <QueryStateCard
          title="Unable to load recent pharmacies"
          description="The recent-pharmacies feed failed. Retry to restore the latest viewed pharmacies."
          onAction={() => recent.refetch()}
        />
      ) : recent.data?.length ? (
        <div className="grid gap-6 md:grid-cols-2">
          {recent.data.map((pharmacy) => (
            <PharmacyFeedCard
              key={pharmacy.pharmacyId}
              pharmacy={pharmacy}
              isPending={toggleFavorite.isPending}
              onToggleFavorite={(input) => toggleFavorite.mutate(input)}
            />
          ))}
        </div>
      ) : (
        <EmptyState
          title="No recent pharmacies yet"
          description="Open a pharmacy detail page and it will appear in this recent activity rail."
          action={(
            <Button asChild>
              <Link href="/pharmacies">Browse pharmacies</Link>
            </Button>
          )}
        />
      )}
    </div>
  )
}
