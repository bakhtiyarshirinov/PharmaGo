import { Card, CardContent, CardHeader } from '@pharmago/ui'

export function PersonalizationSkeleton({ count = 4 }: { count?: number }) {
  return (
    <div className="grid gap-6 md:grid-cols-2">
      {Array.from({ length: count }).map((_, index) => (
        <Card key={index} className="animate-pulse">
          <CardHeader className="space-y-4">
            <div className="h-5 w-1/2 rounded-full bg-slate-200" />
            <div className="h-4 w-2/3 rounded-full bg-slate-100" />
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="h-16 rounded-2xl bg-slate-100" />
            <div className="h-10 rounded-2xl bg-slate-200" />
          </CardContent>
        </Card>
      ))}
    </div>
  )
}
