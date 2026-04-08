'use client'

import Link from 'next/link'
import { Button, EmptyState, PageHeader } from '@pharmago/ui'
import { QueryStateCard } from '../../../../../components/QueryStateCard'
import { useToggleMedicineFavorite } from '../../../../../modules/personalization/medicines/mutations'
import { useRecentMedicines } from '../../../../../modules/personalization/medicines/queries'
import { MedicineFeedCard } from '../../../../../modules/personalization/components/MedicineFeedCard'
import { PersonalizationSkeleton } from '../../../../../modules/personalization/components/PersonalizationSkeleton'

export default function RecentMedicinesPage() {
  const recent = useRecentMedicines()
  const toggleFavorite = useToggleMedicineFavorite()

  return (
    <div className="space-y-8">
      <PageHeader
        eyebrow="Recent medicines"
        title="Everything you viewed recently"
        description="This list helps users return to medicine-first flows without re-searching from scratch."
      />

      {recent.isPending ? (
        <PersonalizationSkeleton />
      ) : recent.isError ? (
        <QueryStateCard
          title="Unable to load recent medicines"
          description="The recent-medicines feed failed. Retry to restore the latest medicine activity."
          onAction={() => recent.refetch()}
        />
      ) : recent.data?.length ? (
        <div className="grid gap-6 md:grid-cols-2">
          {recent.data.map((medicine) => (
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
          title="No recent medicines yet"
          description="Open a medicine detail page and it will appear in this personalized history rail."
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
