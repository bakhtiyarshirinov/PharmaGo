'use client'

import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { mapBackendRole, useAuthStore } from '@pharmago/auth/client'
import type { AuthSession } from '@pharmago/types'
import { Button, Card, CardContent, CardHeader, CardTitle, EmptyState, Input, PageHeader, StatusBadge } from '@pharmago/ui'
import { browserApi } from '../../lib/api'
import { formatDateTime, getUserRoleLabel, getUserRoleTone } from '../../lib/format'
import { queryKeys } from '../../lib/query-keys'

interface UsersScreenProps {
  initialSession?: AuthSession | null
}

export function UsersScreen({ initialSession = null }: UsersScreenProps) {
  const liveSession = useAuthStore((state) => state.session)
  const session = liveSession ?? initialSession
  const role = session?.user.role ? mapBackendRole(session.user.role) : 'guest'
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [roleFilter, setRoleFilter] = useState<'all' | 'consumer' | 'pharmacist'>('all')
  const [activityFilter, setActivityFilter] = useState<'all' | 'active' | 'inactive'>('all')
  const pageSize = 12

  const users = useQuery({
    queryKey: queryKeys.users.list({
      page,
      pageSize,
      search,
      role: roleFilter,
      isActive: activityFilter,
    }),
    queryFn: () =>
      browserApi.admin.users({
        page,
        pageSize,
        search: search.trim() || undefined,
        role: roleFilter === 'all' ? undefined : roleFilter === 'pharmacist' ? 2 : 1,
        isActive: activityFilter === 'all' ? undefined : activityFilter === 'active',
        sortBy: 'createdAt',
        sortDirection: 'desc',
      }),
  })

  if (role !== 'admin') {
    return (
      <EmptyState
        title="Нет доступа к управлению пользователями"
        description="Этот экран доступен только модератору платформы."
      />
    )
  }

  return (
    <div className="space-y-8">
      <PageHeader
        eyebrow="Users"
        title="Пользователи, сотрудники и операционные роли платформы."
        description="Read-only обзор по аккаунтам: фильтрация по роли, активности и аптечной привязке без входа в ручной аудит базы."
        actions={<StatusBadge tone="info">{users.data?.totalCount ?? 0} аккаунтов</StatusBadge>}
      />

      <Card className="admin-glass border-white/60 bg-white/95">
        <CardHeader className="flex flex-col gap-4">
          <div className="space-y-1">
            <CardTitle>Каталог пользователей</CardTitle>
            <p className="text-sm text-slate-500">Поиск по имени, номеру, email и быстрый срез по staff/consumer ролям.</p>
          </div>
          <div className="grid gap-3 lg:grid-cols-[1.2fr,auto,auto]">
            <Input
              value={search}
              onChange={(event) => {
                setPage(1)
                setSearch(event.target.value)
              }}
              placeholder="Поиск по имени, телефону или email"
            />
            <div className="flex flex-wrap gap-2">
              <Button variant={roleFilter === 'all' ? 'primary' : 'outline'} size="sm" onClick={() => {
                setPage(1)
                setRoleFilter('all')
              }}>
                Все роли
              </Button>
              <Button variant={roleFilter === 'consumer' ? 'primary' : 'outline'} size="sm" onClick={() => {
                setPage(1)
                setRoleFilter('consumer')
              }}>
                Пользователи
              </Button>
              <Button variant={roleFilter === 'pharmacist' ? 'primary' : 'outline'} size="sm" onClick={() => {
                setPage(1)
                setRoleFilter('pharmacist')
              }}>
                Фармацевты
              </Button>
            </div>
            <div className="flex flex-wrap gap-2">
              <Button variant={activityFilter === 'all' ? 'primary' : 'outline'} size="sm" onClick={() => {
                setPage(1)
                setActivityFilter('all')
              }}>
                Все
              </Button>
              <Button variant={activityFilter === 'active' ? 'primary' : 'outline'} size="sm" onClick={() => {
                setPage(1)
                setActivityFilter('active')
              }}>
                Активные
              </Button>
              <Button variant={activityFilter === 'inactive' ? 'primary' : 'outline'} size="sm" onClick={() => {
                setPage(1)
                setActivityFilter('inactive')
              }}>
                Выключенные
              </Button>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          {users.isLoading ? (
            <div className="space-y-3">
              {Array.from({ length: 6 }).map((_, index) => (
                <div key={index} className="h-24 animate-pulse rounded-[1.75rem] bg-slate-100" />
              ))}
            </div>
          ) : users.isError ? (
            <div className="rounded-[1.75rem] border border-red-200 bg-red-50 p-5 text-sm text-red-700">
              Не удалось загрузить пользователей.
            </div>
          ) : users.data?.items?.length ? (
            <>
              <div className="overflow-x-auto">
                <table className="min-w-full border-separate border-spacing-y-3 text-sm">
                  <thead>
                    <tr className="text-left text-slate-500">
                      <th className="px-4 py-2 font-medium">Пользователь</th>
                      <th className="px-4 py-2 font-medium">Роль</th>
                      <th className="px-4 py-2 font-medium">Статус</th>
                      <th className="px-4 py-2 font-medium">Аптека</th>
                      <th className="px-4 py-2 font-medium">Создан</th>
                    </tr>
                  </thead>
                  <tbody>
                    {users.data.items.map((user) => (
                      <tr key={user.id} className="align-top">
                        <td className="rounded-l-[1.75rem] border-y border-l border-slate-200/70 bg-white/85 px-4 py-4">
                          <p className="font-semibold text-slate-950">{user.firstName} {user.lastName}</p>
                          <p className="mt-1 text-sm text-slate-500">{user.phoneNumber}</p>
                          {user.email ? <p className="text-xs text-slate-400">{user.email}</p> : null}
                        </td>
                        <td className="border-y border-slate-200/70 bg-white/85 px-4 py-4">
                          <StatusBadge tone={getUserRoleTone(user.role)}>{getUserRoleLabel(user.role)}</StatusBadge>
                        </td>
                        <td className="border-y border-slate-200/70 bg-white/85 px-4 py-4">
                          <StatusBadge tone={user.isActive ? 'success' : 'danger'}>
                            {user.isActive ? 'Активен' : 'Выключен'}
                          </StatusBadge>
                        </td>
                        <td className="border-y border-slate-200/70 bg-white/85 px-4 py-4 text-slate-600">
                          {user.pharmacyName || 'Не привязан'}
                        </td>
                        <td className="rounded-r-[1.75rem] border-y border-r border-slate-200/70 bg-white/85 px-4 py-4 text-slate-600">
                          {formatDateTime(user.createdAtUtc)}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              <div className="mt-4 flex items-center justify-between rounded-[1.75rem] border border-white/70 bg-white/85 px-4 py-3 shadow-[0_18px_50px_rgba(15,23,42,0.06)]">
                <p className="text-sm text-slate-500">
                  Страница {users.data.page} из {users.data.totalPages}
                </p>
                <div className="flex gap-2">
                  <Button variant="outline" size="sm" disabled={users.data.page <= 1} onClick={() => setPage((value) => Math.max(1, value - 1))}>
                    Назад
                  </Button>
                  <Button
                    variant="outline"
                    size="sm"
                    disabled={users.data.page >= users.data.totalPages}
                    onClick={() => setPage((value) => value + 1)}
                  >
                    Дальше
                  </Button>
                </div>
              </div>
            </>
          ) : (
            <EmptyState title="Пользователи не найдены" description="Измени поиск или фильтры роли и активности." />
          )}
        </CardContent>
      </Card>
    </div>
  )
}
