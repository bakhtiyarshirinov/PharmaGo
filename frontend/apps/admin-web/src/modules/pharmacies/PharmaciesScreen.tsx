'use client'

import { useEffect, useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { mapBackendRole, useAuthStore } from '@pharmago/auth/client'
import type { AuthSession, ManagedPharmacy } from '@pharmago/types'
import { Button, Card, CardContent, CardHeader, CardTitle, EmptyState, Input, PageHeader, StatusBadge } from '@pharmago/ui'
import { browserApi } from '../../lib/api'
import { getApiErrorMessage } from '../../lib/errors'
import { formatDateTime, formatNumber } from '../../lib/format'
import { queryKeys } from '../../lib/query-keys'
import { useCreatePharmacy, useDeactivatePharmacy, useRestorePharmacy, useUpdatePharmacy } from './hooks'
import { pharmacyEditorSchema, type PharmacyEditorValues } from './schemas'

interface PharmaciesScreenProps {
  initialSession?: AuthSession | null
}

export function PharmaciesScreen({ initialSession = null }: PharmaciesScreenProps) {
  const liveSession = useAuthStore((state) => state.session)
  const session = liveSession ?? initialSession
  const role = session?.user.role ? mapBackendRole(session.user.role) : 'guest'
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [city, setCity] = useState('')
  const [isActive, setIsActive] = useState<'all' | 'active' | 'inactive'>('all')
  const [supportsReservations, setSupportsReservations] = useState<'all' | 'yes' | 'no'>('all')
  const [selectedPharmacy, setSelectedPharmacy] = useState<ManagedPharmacy | null>(null)
  const [serverError, setServerError] = useState<string | null>(null)
  const pageSize = 12

  const pharmacies = useQuery({
    queryKey: queryKeys.pharmacies.list({
      page,
      pageSize,
      search,
      city,
      isActive,
      supportsReservations,
    }),
    queryFn: () =>
      browserApi.admin.pharmacies({
        page,
        pageSize,
        search: search.trim() || undefined,
        city: city.trim() || undefined,
        isActive: isActive === 'all' ? undefined : isActive === 'active',
        supportsReservations: supportsReservations === 'all' ? undefined : supportsReservations === 'yes',
        sortBy: 'name',
        sortDirection: 'asc',
      }),
  })
  const createPharmacy = useCreatePharmacy()
  const updatePharmacy = useUpdatePharmacy(selectedPharmacy?.id ?? '')
  const deactivatePharmacy = useDeactivatePharmacy()
  const restorePharmacy = useRestorePharmacy()
  const form = useForm<PharmacyEditorValues>({
    resolver: zodResolver(pharmacyEditorSchema),
    defaultValues: {
      name: '',
      address: '',
      city: '',
      region: '',
      phoneNumber: '',
      locationLatitude: undefined,
      locationLongitude: undefined,
      isOpen24Hours: true,
      openingHoursJson: '',
      supportsReservations: true,
      hasDelivery: false,
    },
  })

  useEffect(() => {
    form.reset({
      name: selectedPharmacy?.name ?? '',
      address: selectedPharmacy?.address ?? '',
      city: selectedPharmacy?.city ?? '',
      region: selectedPharmacy?.region ?? '',
      phoneNumber: selectedPharmacy?.phoneNumber ?? '',
      locationLatitude: selectedPharmacy?.locationLatitude ?? undefined,
      locationLongitude: selectedPharmacy?.locationLongitude ?? undefined,
      isOpen24Hours: selectedPharmacy?.isOpen24Hours ?? true,
      openingHoursJson: selectedPharmacy?.openingHoursJson ?? '',
      supportsReservations: selectedPharmacy?.supportsReservations ?? true,
      hasDelivery: selectedPharmacy?.hasDelivery ?? false,
    })
    setServerError(null)
  }, [form, selectedPharmacy])

  const isSubmitting =
    createPharmacy.isPending ||
    updatePharmacy.isPending ||
    deactivatePharmacy.isPending ||
    restorePharmacy.isPending

  const editorTitle = useMemo(
    () => (selectedPharmacy ? 'Редактирование аптеки' : 'Новая аптека'),
    [selectedPharmacy],
  )

  async function onSubmit(values: PharmacyEditorValues) {
    setServerError(null)

    const payload = {
      name: values.name.trim(),
      address: values.address.trim(),
      city: values.city.trim(),
      region: values.region?.trim() || null,
      phoneNumber: values.phoneNumber?.trim() || null,
      locationLatitude: Number.isFinite(values.locationLatitude) ? values.locationLatitude : null,
      locationLongitude: Number.isFinite(values.locationLongitude) ? values.locationLongitude : null,
      isOpen24Hours: values.isOpen24Hours,
      openingHoursJson: values.openingHoursJson?.trim() || null,
      supportsReservations: values.supportsReservations,
      hasDelivery: values.hasDelivery,
      pharmacyChainId: null,
    }

    try {
      if (selectedPharmacy) {
        await updatePharmacy.mutateAsync(payload)
      } else {
        await createPharmacy.mutateAsync(payload)
      }

      setSelectedPharmacy(null)
      form.reset()
    } catch (error) {
      setServerError(getApiErrorMessage(error, 'Не удалось сохранить аптеку.'))
    }
  }

  if (role !== 'admin') {
    return (
      <EmptyState
        title="Нет доступа к управлению аптеками"
        description="Этот экран предназначен для модератора платформы."
      />
    )
  }

  return (
    <div className="space-y-8">
      <PageHeader
        eyebrow="Pharmacies"
        title="Сеть аптек, статусы, нагрузка и операционный профиль."
        description="Таблица собирает живые метрики по каждой точке: команда, активные резервы, склад и готовность к бронированию."
        actions={<StatusBadge tone="info">{pharmacies.data?.totalCount ?? 0} аптек</StatusBadge>}
      />

      <div className="grid gap-6 xl:grid-cols-[1.15fr,0.85fr]">
        <Card className="admin-glass border-white/60 bg-white/95">
          <CardHeader className="flex flex-col gap-4">
            <div className="flex flex-wrap items-start justify-between gap-4">
              <div className="space-y-1">
                <CardTitle>Фильтр и таблица</CardTitle>
                <p className="text-sm text-slate-500">Поиск по названию и адресу, фильтры по городу и операционной доступности.</p>
              </div>
              <Button
                onClick={() => {
                  setSelectedPharmacy(null)
                  form.reset()
                }}
              >
                Создать аптеку
              </Button>
            </div>
            <div className="grid gap-3 lg:grid-cols-[1.2fr,0.9fr,auto,auto]">
              <Input
                value={search}
                onChange={(event) => {
                  setPage(1)
                  setSearch(event.target.value)
                }}
                placeholder="Поиск по названию, адресу или сети"
              />
              <Input
                value={city}
                onChange={(event) => {
                  setPage(1)
                  setCity(event.target.value)
                }}
                placeholder="Город"
              />
              <div className="flex flex-wrap gap-2">
                <Button variant={isActive === 'all' ? 'primary' : 'outline'} size="sm" onClick={() => {
                  setPage(1)
                  setIsActive('all')
                }}>
                  Все
                </Button>
                <Button variant={isActive === 'active' ? 'primary' : 'outline'} size="sm" onClick={() => {
                  setPage(1)
                  setIsActive('active')
                }}>
                  Активные
                </Button>
                <Button variant={isActive === 'inactive' ? 'primary' : 'outline'} size="sm" onClick={() => {
                  setPage(1)
                  setIsActive('inactive')
                }}>
                  Выключенные
                </Button>
              </div>
              <div className="flex flex-wrap gap-2">
                <Button variant={supportsReservations === 'all' ? 'primary' : 'outline'} size="sm" onClick={() => {
                  setPage(1)
                  setSupportsReservations('all')
                }}>
                  Все точки
                </Button>
                <Button variant={supportsReservations === 'yes' ? 'primary' : 'outline'} size="sm" onClick={() => {
                  setPage(1)
                  setSupportsReservations('yes')
                }}>
                  Резервы on
                </Button>
                <Button variant={supportsReservations === 'no' ? 'primary' : 'outline'} size="sm" onClick={() => {
                  setPage(1)
                  setSupportsReservations('no')
                }}>
                  Резервы off
                </Button>
              </div>
            </div>
          </CardHeader>
          <CardContent>
          {pharmacies.isLoading ? (
            <div className="space-y-3">
              {Array.from({ length: 6 }).map((_, index) => (
                <div key={index} className="h-24 animate-pulse rounded-[1.75rem] bg-slate-100" />
              ))}
            </div>
          ) : pharmacies.isError ? (
            <div className="rounded-[1.75rem] border border-red-200 bg-red-50 p-5 text-sm text-red-700">
              Не удалось загрузить список аптек.
            </div>
          ) : pharmacies.data?.items?.length ? (
            <>
              <div className="overflow-x-auto">
                <table className="min-w-full border-separate border-spacing-y-3 text-sm">
                  <thead>
                    <tr className="text-left text-slate-500">
                      <th className="px-4 py-2 font-medium">Аптека</th>
                      <th className="px-4 py-2 font-medium">Статус</th>
                      <th className="px-4 py-2 font-medium">Команда</th>
                      <th className="px-4 py-2 font-medium">Операции</th>
                      <th className="px-4 py-2 font-medium">Контакт</th>
                      <th className="px-4 py-2 font-medium">Обновление</th>
                    </tr>
                  </thead>
                  <tbody>
                    {pharmacies.data.items.map((pharmacy) => (
                      <tr
                        key={pharmacy.id}
                        className="cursor-pointer align-top"
                        onClick={() => setSelectedPharmacy(pharmacy)}
                      >
                        <td className="rounded-l-[1.75rem] border-y border-l border-slate-200/70 bg-white/85 px-4 py-4">
                          <div className="space-y-1">
                            <p className="font-semibold text-slate-950">{pharmacy.name}</p>
                            <p className="text-sm text-slate-500">{pharmacy.city} · {pharmacy.address}</p>
                            {pharmacy.pharmacyChainName ? (
                              <p className="text-xs text-slate-400">{pharmacy.pharmacyChainName}</p>
                            ) : null}
                          </div>
                        </td>
                        <td className="border-y border-slate-200/70 bg-white/85 px-4 py-4">
                          <div className="flex flex-col items-start gap-2">
                            <StatusBadge tone={pharmacy.isActive ? 'success' : 'danger'}>
                              {pharmacy.isActive ? 'Активна' : 'Выключена'}
                            </StatusBadge>
                            <StatusBadge tone={pharmacy.supportsReservations ? 'info' : 'neutral'}>
                              {pharmacy.supportsReservations ? 'Резервы включены' : 'Без резервов'}
                            </StatusBadge>
                          </div>
                        </td>
                        <td className="border-y border-slate-200/70 bg-white/85 px-4 py-4">
                          <p className="font-medium text-slate-950">{formatNumber(pharmacy.employeeCount)}</p>
                          <p className="mt-1 text-xs text-slate-500">Сотрудников</p>
                        </td>
                        <td className="border-y border-slate-200/70 bg-white/85 px-4 py-4">
                          <p className="font-medium text-slate-950">{formatNumber(pharmacy.activeReservationCount)}</p>
                          <p className="mt-1 text-xs text-slate-500">
                            резервов · {formatNumber(pharmacy.activeStockItemCount)} партий
                          </p>
                        </td>
                        <td className="border-y border-slate-200/70 bg-white/85 px-4 py-4">
                          <p className="font-medium text-slate-950">{pharmacy.phoneNumber || 'Нет телефона'}</p>
                          <p className="mt-1 text-xs text-slate-500">
                            {pharmacy.isOpen24Hours ? '24/7' : 'По расписанию'}
                          </p>
                        </td>
                        <td className="rounded-r-[1.75rem] border-y border-r border-slate-200/70 bg-white/85 px-4 py-4">
                          <p className="font-medium text-slate-950">{formatDateTime(pharmacy.updatedAtUtc ?? pharmacy.createdAtUtc)}</p>
                          <p className="mt-1 text-xs text-slate-500">
                            Гео: {pharmacy.lastLocationVerifiedAtUtc ? formatDateTime(pharmacy.lastLocationVerifiedAtUtc) : 'не подтверждено'}
                          </p>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              <div className="mt-4 flex items-center justify-between rounded-[1.75rem] border border-white/70 bg-white/85 px-4 py-3 shadow-[0_18px_50px_rgba(15,23,42,0.06)]">
                <p className="text-sm text-slate-500">
                  Страница {pharmacies.data.page} из {pharmacies.data.totalPages}
                </p>
                <div className="flex gap-2">
                  <Button variant="outline" size="sm" disabled={pharmacies.data.page <= 1} onClick={() => setPage((value) => Math.max(1, value - 1))}>
                    Назад
                  </Button>
                  <Button
                    variant="outline"
                    size="sm"
                    disabled={pharmacies.data.page >= pharmacies.data.totalPages}
                    onClick={() => setPage((value) => value + 1)}
                  >
                    Дальше
                  </Button>
                </div>
              </div>
            </>
          ) : (
            <EmptyState title="Аптеки не найдены" description="Измените фильтры или создайте первую аптеку в системе." />
          )}
          </CardContent>
        </Card>

        <Card className="admin-glass border-white/60 bg-white/95">
          <CardHeader className="space-y-1">
            <CardTitle>{editorTitle}</CardTitle>
            <p className="text-sm text-slate-500">
              {selectedPharmacy
                ? 'Обнови профиль точки, режим работы и операционные флаги.'
                : 'Создай новую точку сети для будущих сотрудников, резерваций и складских остатков.'}
            </p>
          </CardHeader>
          <CardContent>
            <form className="space-y-4" onSubmit={form.handleSubmit(onSubmit)}>
              <Field label="Название" error={form.formState.errors.name?.message}>
                <Input {...form.register('name')} placeholder="PharmaGo Downtown" />
              </Field>
              <Field label="Адрес" error={form.formState.errors.address?.message}>
                <Input {...form.register('address')} placeholder="Nizami street 42" />
              </Field>
              <div className="grid gap-4 md:grid-cols-2">
                <Field label="Город" error={form.formState.errors.city?.message}>
                  <Input {...form.register('city')} placeholder="Baku" />
                </Field>
                <Field label="Регион" error={form.formState.errors.region?.message}>
                  <Input {...form.register('region')} placeholder="Nasimi" />
                </Field>
                <Field label="Телефон" error={form.formState.errors.phoneNumber?.message}>
                  <Input {...form.register('phoneNumber')} placeholder="+994..." />
                </Field>
                <Field label="24/7" error={undefined}>
                  <label className="flex h-11 items-center rounded-[1.25rem] border border-slate-200 px-4">
                    <input type="checkbox" className="mr-3 h-4 w-4" {...form.register('isOpen24Hours')} />
                    Круглосуточно
                  </label>
                </Field>
                <Field label="Широта" error={form.formState.errors.locationLatitude?.message}>
                  <Input type="number" step="0.000001" {...form.register('locationLatitude')} />
                </Field>
                <Field label="Долгота" error={form.formState.errors.locationLongitude?.message}>
                  <Input type="number" step="0.000001" {...form.register('locationLongitude')} />
                </Field>
              </div>
              <Field label="OpeningHours JSON" error={form.formState.errors.openingHoursJson?.message}>
                <textarea
                  className="min-h-28 w-full rounded-[1.25rem] border border-slate-200 px-4 py-3 text-sm outline-none focus:border-teal-500"
                  {...form.register('openingHoursJson')}
                  placeholder='{"timeZone":"Asia/Baku","weekly":[{"day":"Mon","open":"08:00","close":"22:00"}]}'
                />
              </Field>
              <div className="grid gap-3 md:grid-cols-2">
                <label className="flex items-center rounded-[1.25rem] border border-slate-200 px-4 py-3 text-sm">
                  <input type="checkbox" className="mr-3 h-4 w-4" {...form.register('supportsReservations')} />
                  Поддерживает резервы
                </label>
                <label className="flex items-center rounded-[1.25rem] border border-slate-200 px-4 py-3 text-sm">
                  <input type="checkbox" className="mr-3 h-4 w-4" {...form.register('hasDelivery')} />
                  Есть доставка
                </label>
              </div>
              {serverError ? (
                <div className="rounded-[1.25rem] border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
                  {serverError}
                </div>
              ) : null}
              <div className="flex flex-wrap gap-2">
                <Button type="submit" disabled={isSubmitting}>
                  {isSubmitting ? 'Сохраняем...' : selectedPharmacy ? 'Сохранить' : 'Создать аптеку'}
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => {
                    setSelectedPharmacy(null)
                    form.reset()
                  }}
                >
                  Очистить
                </Button>
                {selectedPharmacy ? (
                  selectedPharmacy.isActive ? (
                    <Button
                      type="button"
                      variant="destructive"
                      disabled={isSubmitting}
                      onClick={async () => {
                        try {
                          await deactivatePharmacy.mutateAsync(selectedPharmacy.id)
                          setSelectedPharmacy((current) => current ? { ...current, isActive: false } : current)
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
                          const restored = await restorePharmacy.mutateAsync(selectedPharmacy.id)
                          setSelectedPharmacy(restored)
                        } catch {}
                      }}
                    >
                      Восстановить
                    </Button>
                  )
                ) : null}
              </div>
            </form>
          </CardContent>
        </Card>
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
