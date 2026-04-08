'use client'

import Link from 'next/link'
import { Button, EmptyState, PageHeader } from '@pharmago/ui'
import { QueryStateCard } from '../../../../../components/QueryStateCard'
import { PharmacyFeedCard } from '../../../../../modules/personalization/components/PharmacyFeedCard'
import { PersonalizationSkeleton } from '../../../../../modules/personalization/components/PersonalizationSkeleton'
import { useTogglePharmacyFavorite } from '../../../../../modules/personalization/pharmacies/mutations'
import { useFavoritePharmacies } from '../../../../../modules/personalization/pharmacies/queries'

export default function FavoritePharmaciesPage() {
  const favorites = useFavoritePharmacies()
  const toggleFavorite = useTogglePharmacyFavorite()

  return (
    <div className="space-y-8">
      <PageHeader
        eyebrow="Favorite pharmacies"
        title="Your preferred pharmacy network"
        description="Store-first shoppers can keep the pharmacies they trust closest to their reservation flow."
      />

      {favorites.isPending ? (
        <PersonalizationSkeleton />
      ) : favorites.isError ? (
        <QueryStateCard
          title="Unable to load favorite pharmacies"
          description="The pharmacy favorites feed failed to load. Retry to restore your saved pharmacy shortlist."
          onAction={() => favorites.refetch()}
        />
      ) : favorites.data?.length ? (
        <div className="grid gap-6 md:grid-cols-2">
          {favorites.data.map((pharmacy) => (
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
          title="No favorite pharmacies yet"
          description="Save pharmacies once you find branches with the best service, hours or reservation posture."
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
