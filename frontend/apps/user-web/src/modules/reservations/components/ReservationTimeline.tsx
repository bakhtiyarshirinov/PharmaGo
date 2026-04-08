import type { ReservationTimelineEvent } from '@pharmago/types'
import { Card, CardContent, CardHeader, CardTitle, EmptyState, StatusBadge } from '@pharmago/ui'
import { getReservationStatusLabel, getReservationStatusTone } from '../../../lib/domain'
import { formatDateTime } from '../../../lib/format'

export interface ReservationTimelineProps {
  events: ReservationTimelineEvent[]
}

export function ReservationTimeline({ events }: ReservationTimelineProps) {
  if (!events.length) {
    return <EmptyState title="No timeline yet" description="Reservation lifecycle updates will appear here." />
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>Reservation timeline</CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        {events.map((event, index) => (
          <div key={`${event.action ?? event.title}-${event.occurredAtUtc}-${index}`} className="relative pl-8">
            {index < events.length - 1 ? (
              <span className="absolute left-[9px] top-7 h-[calc(100%+0.5rem)] w-px bg-slate-200" />
            ) : null}
            <span className="absolute left-0 top-1 h-5 w-5 rounded-full border-4 border-stone-50 bg-emerald-600" />
            <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
              <div className="flex flex-col gap-2 md:flex-row md:items-center md:justify-between">
                <div className="space-y-1">
                  <h3 className="font-medium text-slate-950">{event.title}</h3>
                  {event.description ? <p className="text-sm text-slate-600">{event.description}</p> : null}
                </div>
                <div className="flex flex-wrap items-center gap-2">
                  {event.status ? (
                    <StatusBadge tone={getReservationStatusTone(event.status as number)}>
                      {getReservationStatusLabel(event.status as number)}
                    </StatusBadge>
                  ) : null}
                  <span className="text-xs font-medium uppercase tracking-wide text-slate-400">
                    {formatDateTime(event.occurredAtUtc)}
                  </span>
                </div>
              </div>
            </div>
          </div>
        ))}
      </CardContent>
    </Card>
  )
}
