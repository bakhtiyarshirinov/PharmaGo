'use client'

import { useEffect, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { mapBackendRole, useAuthStore } from '@pharmago/auth/client'
import type { AuthSession, ManagedUser } from '@pharmago/types'
import { Button, Card, CardContent, CardHeader, CardTitle, EmptyState, Input, PageHeader, StatusBadge } from '@pharmago/ui'
import { browserApi } from '../../lib/api'
import { getApiErrorMessage } from '../../lib/errors'
import { formatDateTime, getUserRoleLabel, getUserRoleTone } from '../../lib/format'
import { queryKeys } from '../../lib/query-keys'
import { useCreateUser, useDeactivateUser, useRestoreUser, useUpdateUser } from './hooks'
import { userCreateSchema, userUpdateSchema, type UserCreateValues, type UserUpdateValues } from './schemas'

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
  const [selectedUser, setSelectedUser] = useState<ManagedUser | null>(null)
  const [serverError, setServerError] = useState<string | null>(null)
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
  const pharmacies = useQuery({
    queryKey: ['admin', 'users', 'pharmacy-options'],
    queryFn: () => browserApi.admin.pharmacies({ page: 1, pageSize: 100, isActive: true, sortBy: 'name', sortDirection: 'asc' }),
  })
  const createUser = useCreateUser()
  const updateUser = useUpdateUser(selectedUser?.id ?? '')
  const deactivateUser = useDeactivateUser()
  const restoreUser = useRestoreUser()
  const createForm = useForm<UserCreateValues>({
    resolver: zodResolver(userCreateSchema),
    defaultValues: {
      firstName: '',
      lastName: '',
      phoneNumber: '',
      email: '',
      password: '',
      telegramUsername: '',
      telegramChatId: '',
      role: 1,
      pharmacyId: '',
    },
  })
  const updateForm = useForm<UserUpdateValues>({
    resolver: zodResolver(userUpdateSchema),
    defaultValues: {
      firstName: '',
      lastName: '',
      phoneNumber: '',
      email: '',
      password: '',
      telegramUsername: '',
      telegramChatId: '',
      role: 1,
      pharmacyId: '',
    },
  })

  useEffect(() => {
    updateForm.reset({
      firstName: selectedUser?.firstName ?? '',
      lastName: selectedUser?.lastName ?? '',
      phoneNumber: selectedUser?.phoneNumber ?? '',
      email: selectedUser?.email ?? '',
      password: '',
      telegramUsername: selectedUser?.telegramUsername ?? '',
      telegramChatId: selectedUser?.telegramChatId ?? '',
      role: selectedUser?.role === 2 ? 2 : 1,
      pharmacyId: selectedUser?.pharmacyId ?? '',
    })
    setServerError(null)
  }, [selectedUser, updateForm])

  const isSubmitting =
    createUser.isPending ||
    updateUser.isPending ||
    deactivateUser.isPending ||
    restoreUser.isPending

  async function submitCreate(values: UserCreateValues) {
    setServerError(null)

    try {
      await createUser.mutateAsync({
        firstName: values.firstName.trim(),
        lastName: values.lastName.trim(),
        phoneNumber: values.phoneNumber.trim(),
        email: values.email?.trim() || null,
        password: values.password,
        telegramUsername: values.telegramUsername?.trim() || null,
        telegramChatId: values.telegramChatId?.trim() || null,
        role: values.role,
        pharmacyId: values.role === 2 ? values.pharmacyId || null : null,
      })
      createForm.reset()
    } catch (error) {
      setServerError(getApiErrorMessage(error, 'Не удалось создать пользователя.'))
    }
  }

  async function submitUpdate(values: UserUpdateValues) {
    if (!selectedUser) {
      return
    }

    setServerError(null)

    try {
      const updated = await updateUser.mutateAsync({
        firstName: values.firstName.trim(),
        lastName: values.lastName.trim(),
        phoneNumber: values.phoneNumber.trim(),
        email: values.email?.trim() || null,
        password: values.password?.trim() || null,
        telegramUsername: values.telegramUsername?.trim() || null,
        telegramChatId: values.telegramChatId?.trim() || null,
        role: values.role,
        pharmacyId: values.role === 2 ? values.pharmacyId || null : null,
      })
      setSelectedUser(updated)
    } catch (error) {
      setServerError(getApiErrorMessage(error, 'Не удалось обновить пользователя.'))
    }
  }

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

      <div className="grid gap-6 xl:grid-cols-[1.15fr,0.85fr]">
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
                      <tr key={user.id} className="cursor-pointer align-top" onClick={() => setSelectedUser(user)}>
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

        <div className="space-y-6">
          <Card className="admin-glass border-white/60 bg-white/95">
            <CardHeader className="space-y-1">
              <CardTitle>Новый пользователь</CardTitle>
              <p className="text-sm text-slate-500">Создание customer или pharmacist аккаунта прямо из панели модератора.</p>
            </CardHeader>
            <CardContent>
              <form className="space-y-4" onSubmit={createForm.handleSubmit(submitCreate)}>
                <div className="grid gap-4 md:grid-cols-2">
                  <Field label="Имя" error={createForm.formState.errors.firstName?.message}>
                    <Input {...createForm.register('firstName')} />
                  </Field>
                  <Field label="Фамилия" error={createForm.formState.errors.lastName?.message}>
                    <Input {...createForm.register('lastName')} />
                  </Field>
                  <Field label="Телефон" error={createForm.formState.errors.phoneNumber?.message}>
                    <Input {...createForm.register('phoneNumber')} />
                  </Field>
                  <Field label="Email" error={createForm.formState.errors.email?.message}>
                    <Input {...createForm.register('email')} />
                  </Field>
                  <Field label="Пароль" error={createForm.formState.errors.password?.message}>
                    <Input type="password" {...createForm.register('password')} />
                  </Field>
                  <Field label="Роль" error={createForm.formState.errors.role?.message}>
                    <select className="h-11 w-full rounded-[1.25rem] border border-slate-200 px-4" {...createForm.register('role')}>
                      <option value={1}>Пользователь</option>
                      <option value={2}>Фармацевт</option>
                    </select>
                  </Field>
                </div>
                <Field label="Аптека для фармацевта" error={createForm.formState.errors.pharmacyId?.message}>
                  <select className="h-11 w-full rounded-[1.25rem] border border-slate-200 px-4" {...createForm.register('pharmacyId')}>
                    <option value="">Не выбрано</option>
                    {pharmacies.data?.items.map((pharmacy) => (
                      <option key={pharmacy.id} value={pharmacy.id}>{pharmacy.name}</option>
                    ))}
                  </select>
                </Field>
                {serverError ? (
                  <div className="rounded-[1.25rem] border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
                    {serverError}
                  </div>
                ) : null}
                <Button type="submit" disabled={isSubmitting}>
                  {isSubmitting ? 'Сохраняем...' : 'Создать пользователя'}
                </Button>
              </form>
            </CardContent>
          </Card>

          <Card className="admin-glass border-white/60 bg-white/95">
            <CardHeader className="space-y-1">
              <CardTitle>{selectedUser ? 'Редактирование пользователя' : 'Выбери пользователя'}</CardTitle>
              <p className="text-sm text-slate-500">
                {selectedUser ? 'Обнови данные аккаунта, роль или аптечную привязку.' : 'Кликни по строке в таблице слева, чтобы открыть editor.'}
              </p>
            </CardHeader>
            <CardContent>
              {selectedUser ? (
                <form className="space-y-4" onSubmit={updateForm.handleSubmit(submitUpdate)}>
                  <div className="grid gap-4 md:grid-cols-2">
                    <Field label="Имя" error={updateForm.formState.errors.firstName?.message}>
                      <Input {...updateForm.register('firstName')} />
                    </Field>
                    <Field label="Фамилия" error={updateForm.formState.errors.lastName?.message}>
                      <Input {...updateForm.register('lastName')} />
                    </Field>
                    <Field label="Телефон" error={updateForm.formState.errors.phoneNumber?.message}>
                      <Input {...updateForm.register('phoneNumber')} />
                    </Field>
                    <Field label="Email" error={updateForm.formState.errors.email?.message}>
                      <Input {...updateForm.register('email')} />
                    </Field>
                    <Field label="Новый пароль" error={updateForm.formState.errors.password?.message}>
                      <Input type="password" {...updateForm.register('password')} placeholder="Оставь пустым, чтобы не менять" />
                    </Field>
                    <Field label="Роль" error={updateForm.formState.errors.role?.message}>
                      <select className="h-11 w-full rounded-[1.25rem] border border-slate-200 px-4" {...updateForm.register('role')}>
                        <option value={1}>Пользователь</option>
                        <option value={2}>Фармацевт</option>
                      </select>
                    </Field>
                  </div>
                  <Field label="Аптека для фармацевта" error={updateForm.formState.errors.pharmacyId?.message}>
                    <select className="h-11 w-full rounded-[1.25rem] border border-slate-200 px-4" {...updateForm.register('pharmacyId')}>
                      <option value="">Не выбрано</option>
                      {pharmacies.data?.items.map((pharmacy) => (
                        <option key={pharmacy.id} value={pharmacy.id}>{pharmacy.name}</option>
                      ))}
                    </select>
                  </Field>
                  <div className="flex flex-wrap gap-2">
                    <Button type="submit" disabled={isSubmitting}>
                      {isSubmitting ? 'Сохраняем...' : 'Сохранить'}
                    </Button>
                    <Button
                      type="button"
                      variant="outline"
                      onClick={() => setSelectedUser(null)}
                    >
                      Снять выбор
                    </Button>
                    {selectedUser.isActive ? (
                      <Button
                        type="button"
                        variant="destructive"
                        disabled={isSubmitting}
                        onClick={async () => {
                          try {
                            await deactivateUser.mutateAsync(selectedUser.id)
                            setSelectedUser((current) => current ? { ...current, isActive: false } : current)
                          } catch {}
                        }}
                      >
                        Деактивировать
                      </Button>
                    ) : (
                      <Button
                        type="button"
                        variant="secondary"
                        disabled={isSubmitting}
                        onClick={async () => {
                          try {
                            const restored = await restoreUser.mutateAsync(selectedUser.id)
                            setSelectedUser(restored)
                          } catch {}
                        }}
                      >
                        Восстановить
                      </Button>
                    )}
                  </div>
                </form>
              ) : (
                <EmptyState title="Пользователь не выбран" description="Выбери строку из таблицы, чтобы открыть форму редактирования." />
              )}
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  )
}

function Field({
  label,
  error,
  children,
}: {
  label: string
  error?: string
  children: React.ReactNode
}) {
  return (
    <div className="space-y-2">
      <label className="text-sm font-medium text-slate-700">{label}</label>
      {children}
      {error ? <p className="text-sm text-red-600">{error}</p> : null}
    </div>
  )
}
