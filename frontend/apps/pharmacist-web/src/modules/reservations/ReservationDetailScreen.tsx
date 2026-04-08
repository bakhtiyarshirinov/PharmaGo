'use client'

import Link from 'next/link'
import { useMemo } from 'react'
import { useAuthStore } from '@pharmago/auth/client'
import type { AuthSession } from '@pharmago/types'
import { Button, Card, CardContent, CardHeader, CardTitle, EmptyState, PageHeader, StatusBadge } from '@pharmago/ui'
import { canCompleteReservation, formatDateTime, formatMoney, getCompleteGuardMessage, getReservationStatusMeta } from '../../lib/format'
import { useReservationDetail, useReservationTimeline } from './hooks'
import { ReservationActionButtons } from './components/ReservationActionButtons'

interface ReservationDetailScreenProps {
  reservationId: string
  initialSession?: AuthSession | null
}

export function ReservationDetailScreen({ reservationId, initialSession = null }: ReservationDetailScreenProps) {
  const liveSession = useAuthStore((state) => state.session)
  const pharmacyId = liveSession?.user.pharmacyId ?? initialSession?.user.pharmacyId ?? null
  const reservation = useReservationDetail(reservationId)
  const timeline = useReservationTimeline(reservationId)

  const belongsToCurrentPharmacy = useMemo(() => {
    if (!reservation.data || !pharmacyId) {
      return false
    }

    return reservation.data.pharmacyId === pharmacyId
  }, [pharmacyId, reservation.data])

  if (!pharmacyId) {
    return (
      <EmptyState
        title="Нет привязки к аптеке"
        description="Карточка резерва доступна только сотруднику с назначенной аптекой."
      />
    )
  }

  if (reservation.isLoading) {
    return (
      <div className="space-y-6">
        <div className="h-36 animate-pulse rounded-[2rem] bg-slate-100" />
        <div className="h-64 animate-pulse rounded-[2rem] bg-slate-100" />
      </div>
    )
  }

  if (reservation.isError || !reservation.data) {
    return (
      <EmptyState
        title="Карточка резерва недоступна"
        description="Не удалось загрузить детали резерва. Вернитесь в очередь и попробуйте снова."
      />
    )
  }

  if (!belongsToCurrentPharmacy) {
    return (
      <EmptyState
        title="Резерв вне вашей аптеки"
        description="Открыта карточка резерва другой аптеки. Для фармацевта доступен только свой операционный контур."
      />
    )
  }

  const statusMeta = getReservationStatusMeta(reservation.data.status)
  const completeGuard = getCompleteGuardMessage(reservation.data)

  return (
    <div className="space-y-8">
      <PageHeader
        eyebrow="Карточка резерва"
        title={reservation.data.reservationNumber}
        description={`${reservation.data.customerFullName} · ${reservation.data.phoneNumber}`}
        actions={
          <>
            <StatusBadge tone={statusMeta.tone}>{statusMeta.label}</StatusBadge>
            <Button asChild variant="outline">
              <Link href="/reservations">Назад в очередь</Link>
            </Button>
          </>
        }
      />

      <div className="grid gap-6 xl:grid-cols-[1.1fr,0.9fr]">
        <div className="space-y-6">
          <Card className="ops-glass border-white/10 bg-white/95">
            <CardHeader className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
              <div className="space-y-1">
                <CardTitle>Операции по резерву</CardTitle>
                <p className="text-sm text-slate-500">
                  Переводите заказ между этапами смены. Невозможные переходы скрыты заранее, а бизнес-правила все равно проверяет backend.
                </p>
              </div>
              <ReservationActionButtons reservation={reservation.data} />
            </CardHeader>
            <CardContent className="space-y-4">
              {completeGuard && !canCompleteReservation(reservation.data) ? (
                <div className="rounded-[1.5rem] border border-amber-200 bg-amber-50/90 px-4 py-3 text-sm text-amber-700">
                  {completeGuard}
                </div>
              ) : null}

              <div className="grid gap-4 md:grid-cols-2">
                <div className="rounded-[1.75rem] border border-slate-200/80 bg-white/85 p-4 shadow-[0_22px_60px_rgba(15,23,42,0.08)]">
                  <p className="text-xs uppercase tracking-[0.18em] text-slate-400">Создан</p>
                  <p className="mt-2 text-sm font-medium text-slate-950">{formatDateTime(reservation.data.createdAtUtc)}</p>
                </div>
                <div className="rounded-[1.75rem] border border-slate-200/80 bg-white/85 p-4 shadow-[0_22px_60px_rgba(15,23,42,0.08)]">
                  <p className="text-xs uppercase tracking-[0.18em] text-slate-400">Истекает</p>
                  <p className="mt-2 text-sm font-medium text-slate-950">{formatDateTime(reservation.data.reservedUntilUtc)}</p>
                </div>
                <div className="rounded-[1.75rem] border border-slate-200/80 bg-white/85 p-4 shadow-[0_22px_60px_rgba(15,23,42,0.08)]">
                  <p className="text-xs uppercase tracking-[0.18em] text-slate-400">Выдача доступна</p>
                  <p className="mt-2 text-sm font-medium text-slate-950">
                    {formatDateTime(reservation.data.pickupAvailableFromUtc ?? reservation.data.createdAtUtc)}
                  </p>
                </div>
                <div className="rounded-[1.75rem] border border-slate-200/80 bg-white/85 p-4 shadow-[0_22px_60px_rgba(15,23,42,0.08)]">
                  <p className="text-xs uppercase tracking-[0.18em] text-slate-400">Сумма</p>
                  <p className="mt-2 text-sm font-medium text-slate-950">{formatMoney(reservation.data.totalAmount)}</p>
                </div>
              </div>
            </CardContent>
          </Card>

          <Card className="ops-glass border-white/10 bg-white/95">
            <CardHeader>
              <CardTitle>Состав заказа</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              {reservation.data.items.map((item) => (
                <div
                  key={`${reservation.data?.reservationId}-${item.medicineId}`}
                  className="flex items-start justify-between rounded-[1.75rem] border border-slate-200/80 bg-white/85 p-4 shadow-[0_22px_60px_rgba(15,23,42,0.08)]"
                >
                  <div>
                    <p className="font-medium text-slate-950">{item.medicineName}</p>
                    <p className="mt-1 text-sm text-slate-500">{item.genericName || 'Без дженерика'}</p>
                  </div>
                  <div className="text-right">
                    <p className="font-medium text-slate-950">x{item.quantity}</p>
                    <p className="mt-1 text-sm text-slate-500">{formatMoney(item.totalPrice ?? item.unitPrice * item.quantity)}</p>
                  </div>
                </div>
              ))}
            </CardContent>
          </Card>
        </div>

        <div className="space-y-6">
          <Card className="ops-glass border-white/10 bg-white/95">
            <CardHeader>
              <CardTitle>Контекст клиента</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3 text-sm text-slate-600">
              <div className="rounded-[1.75rem] border border-slate-200/80 bg-white/85 p-4 shadow-[0_22px_60px_rgba(15,23,42,0.08)]">
                <p className="font-medium text-slate-950">{reservation.data.customerFullName}</p>
                <p className="mt-1">{reservation.data.phoneNumber}</p>
              </div>
              {reservation.data.notes ? (
                <div className="rounded-[1.75rem] border border-slate-200/80 bg-white/85 p-4 shadow-[0_22px_60px_rgba(15,23,42,0.08)]">
                  <p className="text-xs uppercase tracking-[0.18em] text-slate-400">Комментарий</p>
                  <p className="mt-2 text-slate-700">{reservation.data.notes}</p>
                </div>
              ) : (
                <div className="rounded-[1.75rem] border border-dashed border-slate-200 p-4 text-slate-500">
                  Клиент не оставлял комментарий к заказу.
                </div>
              )}
            </CardContent>
          </Card>

          <Card className="ops-glass border-white/10 bg-white/95">
            <CardHeader>
              <CardTitle>Таймлайн</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              {timeline.isLoading ? (
                <div className="space-y-3">
                  {Array.from({ length: 4 }).map((_, index) => (
                    <div key={index} className="h-16 animate-pulse rounded-3xl bg-slate-100" />
                  ))}
                </div>
              ) : timeline.isError ? (
                <div className="rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
                  Не удалось загрузить таймлайн резерва.
                </div>
              ) : timeline.data?.length ? (
                timeline.data.map((event) => (
                  <div
                    key={`${event.action}-${event.occurredAtUtc}`}
                    className="rounded-[1.75rem] border border-slate-200/80 bg-white/85 p-4 shadow-[0_22px_60px_rgba(15,23,42,0.08)]"
                  >
                    <div className="flex items-start justify-between gap-4">
                      <div>
                        <p className="font-medium text-slate-950">{event.title}</p>
                        <p className="mt-1 text-sm text-slate-500">
                          {event.userFullName ? `${event.userFullName} · ` : ''}
                          {formatDateTime(event.occurredAtUtc)}
                        </p>
                      </div>
                      {event.status ? (
                        <StatusBadge tone={getReservationStatusMeta(event.status).tone}>
                          {getReservationStatusMeta(event.status).label}
                        </StatusBadge>
                      ) : null}
                    </div>
                    {event.description ? <p className="mt-3 text-sm text-slate-600">{event.description}</p> : null}
                  </div>
                ))
              ) : (
                <EmptyState title="Таймлайн пуст" description="Backend еще не вернул событий для этого резерва." />
              )}
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  )
}
