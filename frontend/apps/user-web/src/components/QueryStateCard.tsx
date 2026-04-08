import { AlertTriangle } from 'lucide-react'
import { Button, Card, CardContent, CardHeader, CardTitle } from '@pharmago/ui'

export interface QueryStateCardProps {
  title: string
  description: string
  actionLabel?: string
  onAction?: () => void
}

export function QueryStateCard({ title, description, actionLabel = 'Retry', onAction }: QueryStateCardProps) {
  return (
    <Card className="border-red-100 bg-red-50/60">
      <CardHeader className="flex flex-row items-start gap-3">
        <div className="rounded-2xl bg-white p-2 shadow-sm">
          <AlertTriangle className="h-5 w-5 text-red-600" />
        </div>
        <div className="space-y-1">
          <CardTitle>{title}</CardTitle>
          <p className="text-sm text-slate-600">{description}</p>
        </div>
      </CardHeader>
      {onAction ? (
        <CardContent>
          <Button variant="outline" onClick={onAction}>
            {actionLabel}
          </Button>
        </CardContent>
      ) : null}
    </Card>
  )
}
