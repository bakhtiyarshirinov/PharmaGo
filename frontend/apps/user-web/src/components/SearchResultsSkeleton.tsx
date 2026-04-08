import { Card, CardContent, CardHeader } from '@pharmago/ui'

export interface SearchResultsSkeletonProps {
  count?: number
}

export function SearchResultsSkeleton({ count = 6 }: SearchResultsSkeletonProps) {
  return (
    <div className="grid gap-6 md:grid-cols-2 xl:grid-cols-3">
      {Array.from({ length: count }).map((_, index) => (
        <Card key={index} className="h-full animate-pulse">
          <CardHeader className="space-y-4">
            <div className="h-5 w-2/3 rounded-full bg-slate-200" />
            <div className="h-4 w-1/2 rounded-full bg-slate-100" />
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid gap-3 sm:grid-cols-2">
              <div className="h-20 rounded-2xl bg-slate-100" />
              <div className="h-20 rounded-2xl bg-slate-100" />
            </div>
            <div className="h-11 rounded-2xl bg-slate-200" />
          </CardContent>
        </Card>
      ))}
    </div>
  )
}
