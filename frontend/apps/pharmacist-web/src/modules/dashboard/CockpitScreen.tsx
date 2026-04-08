'use client'

import Link from 'next/link'
import { useQuery } from '@tanstack/react-query'
import { useAuthStore } from '@pharmago/auth/client'
import type { AuthSession } from '@pharmago/types'
import { Button, Card, CardContent, CardHeader, CardTitle, EmptyState, PageHeader, StatusBadge } from '@pharmago/ui'
import { browserApi } from '../../lib/api'
import { formatMoney, formatNumber, getReservationStatusMeta } from '../../lib/format'
import { queryKeys } from '../../lib/query-keys'

function MetricCard({
  title,
  value,
  hint,
  tone = 'neutral',
}: {
  title: string
  value: string
  hint: string
  tone?: 'neutral' | 'success' | 'warning' | 'danger' | 'info'
}) {
  return (
    <Card className="border-0 shadow-lg shadow-slate-950/5">
      <CardHeader className="space-y-3">
        <StatusBadge tone={tone}>{title}</StatusBadge>
        <CardTitle className="text-3xl">{value}</CardTitle>
      </CardHeader>
      <CardContent>
        <p className="text-sm text-slate-500">{hint}</p>
      </CardContent>
    </Card>
  )
}

interface CockpitScreenProps {
  initialSession?: AuthSession | null
}

export function CockpitScreen({ initialSession = null }: CockpitScreenProps) {
  const liveSession = useAuthStore((state) => state.session)
  const session = liveSession ?? initialSession
  const pharmacyId = session?.user.pharmacyId ?? null
  const userName = session ? `${session.user.firstName} ${session.user.lastName}` : 'сотрудника'

  const summary = useQuery({
    queryKey: queryKeys.dashboard.summary(pharmacyId),
    queryFn: () => browserApi.dashboard.summary(pharmacyId ?? undefined),
    enabled: Boolean(pharmacyId),
  })

  const recentReservations = useQuery({
    queryKey: queryKeys.dashboard.recentReservations(pharmacyId),
    queryFn: () => browserApi.dashboard.recentReservations(pharmacyId ?? undefined),
    enabled: Boolean(pharmacyId),
  })

  const unreadNotifications = useQuery({
    queryKey: queryKeys.notifications.unread(5),
    queryFn: () => browserApi.notifications.unread(5),
  })

  const lowStock = useQuery({
    queryKey: queryKeys.inventory.lowStock(pharmacyId),
    queryFn: () => browserApi.stocks.lowStock(pharmacyId ?? undefined),
    enabled: Boolean(pharmacyId),
  })

  const outOfStock = useQuery({
    queryKey: queryKeys.inventory.outOfStock(pharmacyId),
    queryFn: () => browserApi.stocks.outOfStock(pharmacyId ?? undefined),
    enabled: Boolean(pharmacyId),
  })

  const expiring = useQuery({
    queryKey: queryKeys.inventory.expiring(pharmacyId, 21),
    queryFn: () => browserApi.stocks.expiring(21, pharmacyId ?? undefined),
    enabled: Boolean(pharmacyId),
  })

  const restock = useQuery({
    queryKey: queryKeys.inventory.restock(pharmacyId),
    queryFn: () => browserApi.stocks.restockSuggestions(pharmacyId ?? undefined),
    enabled: Boolean(pharmacyId),
  })

  if (!pharmacyId) {
    return (
      <EmptyState
        title="Нет привязки к аптеке"
        description="Кокпит доступен только сотруднику, у которого в профиле назначена аптека."
      />
    )
  }

  return (
    <div className="space-y-8">
      <PageHeader
        eyebrow="Сменный кокпит"
        title="Сначала очередь, потом склад, затем уведомления."
        description={`Операционный обзор для ${userName}: активные резервы, непрочитанные события и давление по остаткам в одном экране.`}
        actions={
          <Button asChild variant="outline">
            <Link href="/reservations">Открыть очередь</Link>
          </Button>
        }
      />

      {summary.isLoading ? (
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          {Array.from({ length: 4 }).map((_, index) => (
            <div key={index} className="h-40 animate-pulse rounded-[2rem] bg-slate-100" />
          ))}
        </div>
      ) : summary.isError || !summary.data ? (
        <div className="rounded-[2rem] border border-red-200 bg-red-50 p-5 text-sm text-red-700">
          Не удалось загрузить сводку по смене.
        </div>
      ) : (
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          <MetricCard
            title="Активные резервы"
            value={formatNumber(summary.data.activeReservations)}
            hint={`${formatNumber(summary.data.readyForPickupReservations)} уже готовы к выдаче.`}
            tone="warning"
          />
          <MetricCard
            title="Непрочитанные уведомления"
            value={formatNumber(unreadNotifications.data?.unreadCount ?? 0)}
            hint="Новые служебные и клиентские события по резервам."
            tone={(unreadNotifications.data?.unreadCount ?? 0) > 0 ? 'info' : 'neutral'}
          />
          <MetricCard
            title="Давление по складу"
            value={formatNumber(summary.data.lowStockAlerts)}
            hint={`${formatNumber(outOfStock.data?.length ?? 0)} полных out-of-stock, ${formatNumber(expiring.data?.length ?? 0)} партий с коротким сроком.`}
            tone={summary.data.lowStockAlerts > 0 ? 'danger' : 'success'}
          />
          <MetricCard
            title="Стоимость активных резервов"
            value={formatMoney(summary.data.reservedValue)}
            hint={`${formatNumber(summary.data.completedToday)} выдач закрыто сегодня.`}
            tone="success"
          />
        </div>
      )}

      <div className="grid gap-6 xl:grid-cols-[1.15fr,0.85fr]">
        <Card>
          <CardHeader className="flex flex-row items-start justify-between gap-4">
            <div className="space-y-1">
              <CardTitle>Последние резервы</CardTitle>
              <p className="text-sm text-slate-500">Быстрый вход в карточку заказа и текущий статус по каждому новому резерву.</p>
            </div>
            <Button asChild variant="outline" size="sm">
              <Link href="/reservations">Вся очередь</Link>
            </Button>
          </CardHeader>
          <CardContent className="space-y-3">
            {recentReservations.isLoading ? (
              Array.from({ length: 5 }).map((_, index) => (
                <div key={index} className="h-20 animate-pulse rounded-3xl bg-slate-100" />
              ))
            ) : recentReservations.data?.length ? (
              recentReservations.data.map((reservation) => {
                const statusMeta = getReservationStatusMeta(reservation.status)

                return (
                  <div key={reservation.reservationId} className="rounded-3xl border border-slate-200 bg-slate-50 p-4">
                    <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
                      <div className="space-y-1">
                        <Link href={`/reservations/${reservation.reservationId}`} className="font-semibold text-slate-950 hover:text-emerald-700">
                          {reservation.reservationNumber}
                        </Link>
                        <p className="text-sm text-slate-500">
                          {reservation.customerFullName} · {formatMoney(reservation.totalAmount)}
                        </p>
                      </div>
                      <div className="flex items-center gap-3">
                        <StatusBadge tone={statusMeta.tone}>{statusMeta.label}</StatusBadge>
                        <Button asChild variant="outline" size="sm">
                          <Link href={`/reservations/${reservation.reservationId}`}>Открыть</Link>
                        </Button>
                      </div>
                    </div>
                  </div>
                )
              })
            ) : (
              <EmptyState title="Новых резервов нет" description="Как только появятся новые брони, они появятся здесь." />
            )}
          </CardContent>
        </Card>

        <div className="space-y-6">
          <Card>
            <CardHeader className="flex flex-row items-start justify-between gap-4">
              <div className="space-y-1">
                <CardTitle>Непрочитанные уведомления</CardTitle>
                <p className="text-sm text-slate-500">Снимок последних непрочитанных событий по смене.</p>
              </div>
              <Button asChild variant="outline" size="sm">
                <Link href="/notifications">Открыть inbox</Link>
              </Button>
            </CardHeader>
            <CardContent className="space-y-3">
              {unreadNotifications.data?.previewItems?.length ? (
                unreadNotifications.data.previewItems.slice(0, 3).map((item) => (
                  <div key={item.notificationId} className="rounded-3xl bg-slate-50 p-4">
                    <p className="font-medium text-slate-950">{item.title}</p>
                    <p className="mt-1 text-sm text-slate-500">{item.message}</p>
                  </div>
                ))
              ) : (
                <EmptyState title="Inbox спокоен" description="Сейчас нет непрочитанных служебных уведомлений." />
              )}
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="flex flex-row items-start justify-between gap-4">
              <div className="space-y-1">
                <CardTitle>Снимок складского давления</CardTitle>
                <p className="text-sm text-slate-500">Куда смотреть первым делом в смене.</p>
              </div>
              <Button asChild variant="outline" size="sm">
                <Link href="/inventory">Открыть склад</Link>
              </Button>
            </CardHeader>
            <CardContent className="space-y-3">
              <div className="rounded-3xl bg-slate-50 p-4">
                <p className="text-sm text-slate-500">Low stock</p>
                <p className="mt-1 text-2xl font-semibold text-slate-950">{formatNumber(lowStock.data?.length ?? 0)}</p>
              </div>
              <div className="rounded-3xl bg-slate-50 p-4">
                <p className="text-sm text-slate-500">Out of stock</p>
                <p className="mt-1 text-2xl font-semibold text-slate-950">{formatNumber(outOfStock.data?.length ?? 0)}</p>
              </div>
              <div className="rounded-3xl bg-slate-50 p-4">
                <p className="text-sm text-slate-500">Скоро истекают</p>
                <p className="mt-1 text-2xl font-semibold text-slate-950">{formatNumber(expiring.data?.length ?? 0)}</p>
              </div>
              <div className="rounded-3xl bg-slate-50 p-4">
                <p className="text-sm text-slate-500">Restock suggestions</p>
                <p className="mt-1 text-2xl font-semibold text-slate-950">{formatNumber(restock.data?.length ?? 0)}</p>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  )
}
