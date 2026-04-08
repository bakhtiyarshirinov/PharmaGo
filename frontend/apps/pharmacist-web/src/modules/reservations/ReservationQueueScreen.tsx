'use client'

import Link from 'next/link'
import { useMemo, useState } from 'react'
import { mapBackendRole, useAuthStore } from '@pharmago/auth/client'
import type { AuthSession } from '@pharmago/types'
import { Button, Card, CardContent, CardHeader, CardTitle, EmptyState, Input, PageHeader, StatusBadge } from '@pharmago/ui'
import { formatDateTime, formatMoney, getReservationCountdown, getReservationStatusMeta } from '../../lib/format'
import { useReservationsByPharmacy } from './hooks'
import { ReservationActionButtons } from './components/ReservationActionButtons'

type QueueFilter = 'active' | 'pending' | 'confirmed' | 'ready' | 'closed'

const filterLabels: Record<QueueFilter, string> = {
  active: 'Активные',
  pending: 'Ожидают',
  confirmed: 'Подтверждены',
  ready: 'Готовы',
  closed: 'Закрытые',
}

interface ReservationQueueScreenProps {
  initialSession?: AuthSession | null
}

export function ReservationQueueScreen({ initialSession = null }: ReservationQueueScreenProps) {
  const liveSession = useAuthStore((state) => state.session)
  const session = liveSession ?? initialSession
  const pharmacyId = session?.user.pharmacyId ?? null
  const userName = session ? `${session.user.firstName} ${session.user.lastName}` : null
  const role = session?.user.role ? mapBackendRole(session.user.role) : 'guest'
  const canManageReservations = role === 'pharmacist' || role === 'admin'
  const [search, setSearch] = useState('')
  const [filter, setFilter] = useState<QueueFilter>('active')
  const reservations = useReservationsByPharmacy(pharmacyId)

  const filteredReservations = useMemo(() => {
    const query = search.trim().toLowerCase()

    return (reservations.data ?? [])
      .filter((reservation) => {
        if (filter === 'active') {
          return reservation.status === 1 || reservation.status === 2 || reservation.status === 3
        }

        if (filter === 'pending') {
          return reservation.status === 1
        }

        if (filter === 'confirmed') {
          return reservation.status === 2
        }

        if (filter === 'ready') {
          return reservation.status === 3
        }

        return reservation.status === 4 || reservation.status === 5 || reservation.status === 6
      })
      .filter((reservation) => {
        if (!query) {
          return true
        }

        const haystack = [
          reservation.reservationNumber,
          reservation.customerFullName,
          reservation.phoneNumber,
          reservation.items.map((item) => item.medicineName).join(' '),
        ]
          .join(' ')
          .toLowerCase()

        return haystack.includes(query)
      })
  }, [filter, reservations.data, search])

  if (!canManageReservations) {
    return (
      <EmptyState
        title="Нет доступа к очереди резервов"
        description="Для работы с очередью требуется роль фармацевта или модератора."
      />
    )
  }

  if (!pharmacyId) {
    return (
      <EmptyState
        title="Аптека не привязана"
        description="У текущего сотрудника не назначена аптека, поэтому очередь резервов недоступна."
      />
    )
  }

  return (
    <div className="space-y-8">
      <PageHeader
        eyebrow="Очередь резервов"
        title="Подтверждение и выдача без лишних переходов."
        description={`Рабочая очередь для ${userName ?? 'сотрудника аптеки'}: сначала активные резервы, затем детализация по клиенту и позиции заказа.`}
        actions={<StatusBadge tone="info">{filteredReservations.length} в текущем фильтре</StatusBadge>}
      />

      <Card>
        <CardHeader className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
          <div className="space-y-1">
            <CardTitle>Фильтр очереди</CardTitle>
            <p className="text-sm text-slate-500">Быстрый переход между статусами и поиск по клиенту, номеру резерва и составу заказа.</p>
          </div>
          <div className="w-full max-w-md">
            <Input
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Поиск по номеру, клиенту или лекарству"
            />
          </div>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex flex-wrap gap-2">
            {(Object.keys(filterLabels) as QueueFilter[]).map((key) => (
              <Button
                key={key}
                variant={filter === key ? 'primary' : 'outline'}
                size="sm"
                onClick={() => setFilter(key)}
              >
                {filterLabels[key]}
              </Button>
            ))}
          </div>

          {reservations.isLoading ? (
            <div className="space-y-3">
              {Array.from({ length: 5 }).map((_, index) => (
                <div key={index} className="h-20 animate-pulse rounded-3xl bg-slate-100" />
              ))}
            </div>
          ) : reservations.isError ? (
            <div className="rounded-3xl border border-red-200 bg-red-50 p-5 text-sm text-red-700">
              Не удалось загрузить очередь резервов. Обновите страницу или попробуйте позже.
            </div>
          ) : filteredReservations.length === 0 ? (
            <EmptyState
              title="Резервы не найдены"
              description="В этой выборке пока нет записей. Попробуйте другой фильтр или снимите поиск."
            />
          ) : (
            <div className="overflow-x-auto">
              <table className="min-w-full border-separate border-spacing-y-3 text-sm">
                <thead>
                  <tr className="text-left text-slate-500">
                    <th className="px-4 py-2 font-medium">Резерв</th>
                    <th className="px-4 py-2 font-medium">Клиент</th>
                    <th className="px-4 py-2 font-medium">Позиции</th>
                    <th className="px-4 py-2 font-medium">Срок</th>
                    <th className="px-4 py-2 font-medium">Сумма</th>
                    <th className="px-4 py-2 font-medium">Статус</th>
                    <th className="px-4 py-2 font-medium">Действия</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredReservations.map((reservation) => {
                    const statusMeta = getReservationStatusMeta(reservation.status)

                    return (
                      <tr key={reservation.reservationId} className="rounded-3xl bg-slate-50 align-top">
                        <td className="rounded-l-3xl px-4 py-4">
                          <div className="space-y-1">
                            <Link
                              href={`/reservations/${reservation.reservationId}`}
                              className="font-semibold text-slate-950 hover:text-emerald-700"
                            >
                              {reservation.reservationNumber}
                            </Link>
                            <p className="text-xs text-slate-500">{formatDateTime(reservation.createdAtUtc)}</p>
                          </div>
                        </td>
                        <td className="px-4 py-4">
                          <div className="space-y-1">
                            <p className="font-medium text-slate-900">{reservation.customerFullName}</p>
                            <p className="text-xs text-slate-500">{reservation.phoneNumber}</p>
                          </div>
                        </td>
                        <td className="px-4 py-4">
                          <div className="space-y-1">
                            {reservation.items.slice(0, 2).map((item) => (
                              <p key={`${reservation.reservationId}-${item.medicineId}`} className="text-slate-700">
                                {item.medicineName} x{item.quantity}
                              </p>
                            ))}
                            {reservation.items.length > 2 ? (
                              <p className="text-xs text-slate-500">Еще позиций: {reservation.items.length - 2}</p>
                            ) : null}
                          </div>
                        </td>
                        <td className="px-4 py-4">
                          <div className="space-y-1">
                            <p className="font-medium text-slate-900">{formatDateTime(reservation.reservedUntilUtc)}</p>
                            <p className="text-xs text-slate-500">{getReservationCountdown(reservation)}</p>
                          </div>
                        </td>
                        <td className="px-4 py-4 font-medium text-slate-900">{formatMoney(reservation.totalAmount)}</td>
                        <td className="px-4 py-4">
                          <StatusBadge tone={statusMeta.tone}>{statusMeta.label}</StatusBadge>
                        </td>
                        <td className="rounded-r-3xl px-4 py-4">
                          <div className="space-y-3">
                            <ReservationActionButtons reservation={reservation} compact />
                            <Button asChild variant="outline" size="sm">
                              <Link href={`/reservations/${reservation.reservationId}`}>Открыть карточку</Link>
                            </Button>
                          </div>
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
