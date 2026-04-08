'use client'

import Link from 'next/link'
import { Button, EmptyState, PageHeader } from '@pharmago/ui'
import { QueryStateCard } from '../../../../../components/QueryStateCard'
import { useFavoriteMedicines } from '../../../../../modules/personalization/medicines/queries'
import { useToggleMedicineFavorite } from '../../../../../modules/personalization/medicines/mutations'
import { MedicineFeedCard } from '../../../../../modules/personalization/components/MedicineFeedCard'
import { PersonalizationSkeleton } from '../../../../../modules/personalization/components/PersonalizationSkeleton'

export default function FavoriteMedicinesPage() {
  const favorites = useFavoriteMedicines()
  const toggleFavorite = useToggleMedicineFavorite()

  return (
    <div className="space-y-8">
      <PageHeader
        eyebrow="Favorite medicines"
        title="Your saved medicine shortlist"
        description="Use this rail to revisit medicines you intentionally want to monitor or reserve later."
      />

      {favorites.isPending ? (
        <PersonalizationSkeleton />
      ) : favorites.isError ? (
        <QueryStateCard
          title="Unable to load favorite medicines"
          description="The favorites feed failed to load. Retry to restore your saved medicine shortlist."
          onAction={() => favorites.refetch()}
        />
      ) : favorites.data?.length ? (
        <div className="grid gap-6 md:grid-cols-2">
          {favorites.data.map((medicine) => (
            <MedicineFeedCard
              key={medicine.medicineId}
              medicine={medicine}
              isPending={toggleFavorite.isPending}
              onToggleFavorite={(input) => toggleFavorite.mutate(input)}
            />
          ))}
        </div>
      ) : (
        <EmptyState
          title="No favorite medicines yet"
          description="Save medicines from search or detail pages once you want to track them more closely."
          action={(
            <Button asChild>
              <Link href="/medicines">Browse medicines</Link>
            </Button>
          )}
        />
      )}
    </div>
  )
}
