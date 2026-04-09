'use client'

import Link from 'next/link'
import { mapBackendRole, useAuthStore } from '@pharmago/auth/client'
import type { AuthSession } from '@pharmago/types'
import { Button, Card, CardContent, CardHeader, CardTitle, EmptyState, PageHeader, StatusBadge } from '@pharmago/ui'
import { formatDateTime, formatMoney, formatNumber, getUserRoleLabel, getUserRoleTone } from '../../lib/format'
import {
  useAdminPharmaciesPreview,
  useAdminRecentReservations,
  useAdminSummary,
  useAdminUsersPreview,
} from './hooks'

interface OverviewScreenProps {
  initialSession?: AuthSession | null
}

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
    <Card className="admin-glass border-white/60 bg-white/95 shadow-xl shadow-slate-950/5">
      <CardHeader className="space-y-3">
        <StatusBadge tone={tone}>{title}</StatusBadge>
        <CardTitle className="admin-display text-4xl text-slate-950">{value}</CardTitle>
      </CardHeader>
      <CardContent>
        <p className="text-sm text-slate-500">{hint}</p>
      </CardContent>
    </Card>
  )
}

export function OverviewScreen({ initialSession = null }: OverviewScreenProps) {
  const liveSession = useAuthStore((state) => state.session)
  const session = liveSession ?? initialSession
  const role = session?.user.role ? mapBackendRole(session.user.role) : 'guest'
  const summary = useAdminSummary()
  const recentReservations = useAdminRecentReservations()
  const usersPreview = useAdminUsersPreview()
  const pharmaciesPreview = useAdminPharmaciesPreview()

  if (role !== 'admin') {
    return (
      <EmptyState
        title="Нет доступа к admin overview"
        description="Этот экран доступен только модератору платформы."
      />
    )
  }

  return (
    <div className="space-y-8">
      <PageHeader
        eyebrow="Admin overview"
        title="Глобальный контроль платформы без переключения между контурами."
        description="Сводка по всей сети: пользователи, аптеки, активные резервы, stock pressure и последние операционные события."
        actions={
          <Button asChild variant="outline">
            <Link href="/pharmacies">Открыть аптеки</Link>
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
          Не удалось загрузить admin overview.
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
            title="Аптеки"
            value={formatNumber(summary.data.totalPharmacies)}
            hint={`${formatNumber(summary.data.lowStockAlerts)} точек с low-stock давлением.`}
            tone="info"
          />
          <MetricCard
            title="Пользователи"
            value={formatNumber(summary.data.totalUsers)}
            hint={`${formatNumber(summary.data.totalStockItems)} активных складских партий в системе.`}
            tone="neutral"
          />
          <MetricCard
            title="Зарезервированная стоимость"
            value={formatMoney(summary.data.reservedValue)}
            hint={`${formatNumber(summary.data.completedToday)} выдач закрыто сегодня.`}
            tone="success"
          />
        </div>
      )}

      <div className="grid gap-6 xl:grid-cols-[1.15fr,0.85fr]">
        <Card className="admin-glass border-white/60 bg-white/95">
          <CardHeader className="flex flex-row items-start justify-between gap-4">
            <div className="space-y-1">
              <CardTitle>Последние резервы</CardTitle>
              <p className="text-sm text-slate-500">Глобальный взгляд на последние клиентские заказы по всей сети.</p>
            </div>
            <StatusBadge tone="info">Global feed</StatusBadge>
          </CardHeader>
          <CardContent className="space-y-3">
            {recentReservations.isLoading ? (
              Array.from({ length: 5 }).map((_, index) => (
                <div key={index} className="h-20 animate-pulse rounded-[1.75rem] bg-slate-100" />
              ))
            ) : recentReservations.data?.length ? (
              recentReservations.data.map((reservation) => (
                <div
                  key={reservation.reservationId}
                  className="rounded-[1.75rem] border border-white/70 bg-white/85 p-4 shadow-[0_18px_50px_rgba(15,23,42,0.08)]"
                >
                  <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
                    <div className="space-y-1">
                      <p className="font-semibold text-slate-950">{reservation.reservationNumber}</p>
                      <p className="text-sm text-slate-500">
                        {reservation.customerFullName} · {reservation.pharmacyName}
                      </p>
                    </div>
                    <div className="text-right text-sm text-slate-500">
                      <p className="font-medium text-slate-950">{formatMoney(reservation.totalAmount)}</p>
                      <p>{formatDateTime(reservation.createdAtUtc)}</p>
                    </div>
                  </div>
                </div>
              ))
            ) : (
              <EmptyState title="Резервов пока нет" description="Последние заказы появятся здесь автоматически." />
            )}
          </CardContent>
        </Card>

        <div className="space-y-6">
          <Card className="admin-glass border-white/60 bg-white/95">
            <CardHeader className="flex flex-row items-start justify-between gap-4">
              <div className="space-y-1">
                <CardTitle>Новые пользователи</CardTitle>
                <p className="text-sm text-slate-500">Последние учетные записи в системе.</p>
              </div>
              <Button asChild variant="outline" size="sm">
                <Link href="/users">Все пользователи</Link>
              </Button>
            </CardHeader>
            <CardContent className="space-y-3">
              {usersPreview.data?.items?.length ? (
                usersPreview.data.items.map((user) => (
                  <div
                    key={user.id}
                    className="flex items-center justify-between rounded-[1.5rem] border border-white/70 bg-white/85 p-4 shadow-[0_18px_50px_rgba(15,23,42,0.06)]"
                  >
                    <div>
                      <p className="font-medium text-slate-950">{user.firstName} {user.lastName}</p>
                      <p className="text-sm text-slate-500">{user.phoneNumber}</p>
                    </div>
                    <StatusBadge tone={getUserRoleTone(user.role)}>{getUserRoleLabel(user.role)}</StatusBadge>
                  </div>
                ))
              ) : (
                <EmptyState title="Пользователей нет" description="Новые учетные записи появятся в этом блоке." />
              )}
            </CardContent>
          </Card>

          <Card className="admin-glass border-white/60 bg-white/95">
            <CardHeader className="flex flex-row items-start justify-between gap-4">
              <div className="space-y-1">
                <CardTitle>Аптеки сети</CardTitle>
                <p className="text-sm text-slate-500">Быстрый снимок по ключевым точкам.</p>
              </div>
              <Button asChild variant="outline" size="sm">
                <Link href="/pharmacies">К таблице</Link>
              </Button>
            </CardHeader>
            <CardContent className="space-y-3">
              {pharmaciesPreview.data?.items?.length ? (
                pharmaciesPreview.data.items.map((pharmacy) => (
                  <div
                    key={pharmacy.id}
                    className="rounded-[1.5rem] border border-white/70 bg-white/85 p-4 shadow-[0_18px_50px_rgba(15,23,42,0.06)]"
                  >
                    <div className="flex items-center justify-between gap-3">
                      <p className="font-medium text-slate-950">{pharmacy.name}</p>
                      <StatusBadge tone={pharmacy.isActive ? 'success' : 'danger'}>
                        {pharmacy.isActive ? 'Активна' : 'Выключена'}
                      </StatusBadge>
                    </div>
                    <p className="mt-1 text-sm text-slate-500">
                      {pharmacy.city} · сотрудников {formatNumber(pharmacy.employeeCount)} · активных резервов {formatNumber(pharmacy.activeReservationCount)}
                    </p>
                  </div>
                ))
              ) : (
                <EmptyState title="Аптек пока нет" description="После создания аптек здесь появится быстрый превью-лист." />
              )}
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  )
}
