'use client'

import { useEffect, useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { mapBackendRole, useAuthStore } from '@pharmago/auth/client'
import type { AuthSession, ManagedMedicine, ManagedMedicineCategory } from '@pharmago/types'
import { Button, Card, CardContent, CardHeader, CardTitle, EmptyState, Input, PageHeader, StatusBadge } from '@pharmago/ui'
import { browserApi } from '../../lib/api'
import { getApiErrorMessage } from '../../lib/errors'
import { formatNumber, getActiveStateLabel, getActiveStateTone } from '../../lib/format'
import { queryKeys } from '../../lib/query-keys'
import {
  useCreateMedicine,
  useCreateMedicineCategory,
  useUpdateMedicine,
  useUpdateMedicineCategory,
} from './hooks'
import {
  medicineCategoryEditorSchema,
  medicineEditorSchema,
  type MedicineCategoryEditorValues,
  type MedicineEditorValues,
} from './schemas'

interface MedicinesScreenProps {
  initialSession?: AuthSession | null
}

export function MedicinesScreen({ initialSession = null }: MedicinesScreenProps) {
  const liveSession = useAuthStore((state) => state.session)
  const session = liveSession ?? initialSession
  const role = session?.user.role ? mapBackendRole(session.user.role) : 'guest'
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [categoryFilter, setCategoryFilter] = useState('')
  const [activityFilter, setActivityFilter] = useState<'all' | 'active' | 'inactive'>('all')
  const [categorySearch, setCategorySearch] = useState('')
  const [selectedMedicine, setSelectedMedicine] = useState<ManagedMedicine | null>(null)
  const [selectedCategory, setSelectedCategory] = useState<ManagedMedicineCategory | null>(null)
  const [medicineError, setMedicineError] = useState<string | null>(null)
  const [categoryError, setCategoryError] = useState<string | null>(null)
  const pageSize = 12

  const categories = useQuery({
    queryKey: queryKeys.masterData.categories({
      page: 1,
      pageSize: 100,
      search: categorySearch,
    }),
    queryFn: () =>
      browserApi.admin.medicineCategories({
        page: 1,
        pageSize: 100,
        search: categorySearch.trim() || undefined,
        sortDirection: 'asc',
      }),
  })

  const medicines = useQuery({
    queryKey: queryKeys.masterData.medicines({
      page,
      pageSize,
      search,
      categoryId: categoryFilter,
      isActive: activityFilter,
    }),
    queryFn: () =>
      browserApi.admin.medicines({
        page,
        pageSize,
        search: search.trim() || undefined,
        categoryId: categoryFilter || undefined,
        isActive: activityFilter === 'all' ? undefined : activityFilter === 'active',
        sortBy: 'brandName',
        sortDirection: 'asc',
      }),
  })

  const createMedicine = useCreateMedicine()
  const updateMedicine = useUpdateMedicine(selectedMedicine?.id ?? '')
  const createCategory = useCreateMedicineCategory()
  const updateCategory = useUpdateMedicineCategory(selectedCategory?.id ?? '')

  const medicineForm = useForm<MedicineEditorValues>({
    resolver: zodResolver(medicineEditorSchema),
    defaultValues: {
      brandName: '',
      genericName: '',
      description: '',
      dosageForm: '',
      strength: '',
      manufacturer: '',
      countryOfOrigin: '',
      barcode: '',
      requiresPrescription: false,
      isActive: true,
      categoryId: '',
    },
  })

  const categoryForm = useForm<MedicineCategoryEditorValues>({
    resolver: zodResolver(medicineCategoryEditorSchema),
    defaultValues: {
      name: '',
      description: '',
    },
  })

  useEffect(() => {
    medicineForm.reset({
      brandName: selectedMedicine?.brandName ?? '',
      genericName: selectedMedicine?.genericName ?? '',
      description: selectedMedicine?.description ?? '',
      dosageForm: selectedMedicine?.dosageForm ?? '',
      strength: selectedMedicine?.strength ?? '',
      manufacturer: selectedMedicine?.manufacturer ?? '',
      countryOfOrigin: selectedMedicine?.countryOfOrigin ?? '',
      barcode: selectedMedicine?.barcode ?? '',
      requiresPrescription: selectedMedicine?.requiresPrescription ?? false,
      isActive: selectedMedicine?.isActive ?? true,
      categoryId: selectedMedicine?.categoryId ?? '',
    })
    setMedicineError(null)
  }, [medicineForm, selectedMedicine])

  useEffect(() => {
    categoryForm.reset({
      name: selectedCategory?.name ?? '',
      description: selectedCategory?.description ?? '',
    })
    setCategoryError(null)
  }, [categoryForm, selectedCategory])

  const isSubmitting =
    createMedicine.isPending ||
    updateMedicine.isPending ||
    createCategory.isPending ||
    updateCategory.isPending

  const selectedCategoryName = useMemo(() => {
    if (!categoryFilter) {
      return 'Все категории'
    }

    return categories.data?.items.find((item) => item.id === categoryFilter)?.name ?? 'Выбранная категория'
  }, [categories.data?.items, categoryFilter])

  async function submitMedicine(values: MedicineEditorValues) {
    setMedicineError(null)

    const payload = {
      brandName: values.brandName.trim(),
      genericName: values.genericName.trim(),
      description: values.description?.trim() || null,
      dosageForm: values.dosageForm.trim(),
      strength: values.strength.trim(),
      manufacturer: values.manufacturer.trim(),
      countryOfOrigin: values.countryOfOrigin?.trim() || null,
      barcode: values.barcode?.trim() || null,
      requiresPrescription: values.requiresPrescription,
      isActive: values.isActive,
      categoryId: values.categoryId || null,
    }

    try {
      const saved = selectedMedicine
        ? await updateMedicine.mutateAsync(payload)
        : await createMedicine.mutateAsync(payload)

      setSelectedMedicine(saved)
      if (saved.categoryId) {
        setCategoryFilter(saved.categoryId)
      }
    } catch (error) {
      setMedicineError(getApiErrorMessage(error, 'Не удалось сохранить карточку лекарства.'))
    }
  }

  async function submitCategory(values: MedicineCategoryEditorValues) {
    setCategoryError(null)

    const payload = {
      name: values.name.trim(),
      description: values.description?.trim() || null,
    }

    try {
      const saved = selectedCategory
        ? await updateCategory.mutateAsync(payload)
        : await createCategory.mutateAsync(payload)

      setSelectedCategory(saved)
      if (!categoryFilter) {
        setCategoryFilter(saved.id)
      }
    } catch (error) {
      setCategoryError(getApiErrorMessage(error, 'Не удалось сохранить категорию.'))
    }
  }

  if (role !== 'admin') {
    return (
      <EmptyState
        title="Нет доступа к master-data"
        description="Каталогом лекарств и категориями управляет только модератор платформы."
      />
    )
  }

  return (
    <div className="space-y-8">
      <PageHeader
        eyebrow="Master data"
        title="Каталог лекарств и справочник категорий."
        description="Здесь живет платформа: карточки лекарств, рецептурность, barcode, активность и базовые категории для всего продукта."
        actions={<StatusBadge tone="info">{medicines.data?.totalCount ?? 0} лекарств</StatusBadge>}
      />

      <div className="grid gap-6 xl:grid-cols-[1.15fr,0.85fr]">
        <Card className="admin-glass border-white/60 bg-white/95">
          <CardHeader className="flex flex-col gap-4">
            <div className="flex flex-wrap items-start justify-between gap-4">
              <div className="space-y-1">
                <CardTitle>Каталог лекарств</CardTitle>
                <p className="text-sm text-slate-500">Поиск по бренду, дженерику, barcode и производителю с быстрым переходом в edit-панель.</p>
              </div>
              <Button
                onClick={() => {
                  setSelectedMedicine(null)
                  medicineForm.reset()
                }}
              >
                Новое лекарство
              </Button>
            </div>
            <div className="grid gap-3 lg:grid-cols-[1.2fr,0.9fr,auto]">
              <Input
                value={search}
                onChange={(event) => {
                  setPage(1)
                  setSearch(event.target.value)
                }}
                placeholder="Поиск по бренду, дженерику, barcode, производителю"
              />
              <div className="rounded-[1.25rem] border border-slate-200/70 bg-white/80 px-4 py-3 text-sm text-slate-700">
                Категория: <span className="font-medium text-slate-950">{selectedCategoryName}</span>
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
            {medicines.isLoading ? (
              <div className="space-y-3">
                {Array.from({ length: 6 }).map((_, index) => (
                  <div key={index} className="h-24 animate-pulse rounded-[1.75rem] bg-slate-100" />
                ))}
              </div>
            ) : medicines.isError ? (
              <div className="rounded-[1.75rem] border border-red-200 bg-red-50 p-5 text-sm text-red-700">
                Не удалось загрузить каталог лекарств.
              </div>
            ) : medicines.data?.items?.length ? (
              <>
                <div className="overflow-x-auto">
                  <table className="min-w-full border-separate border-spacing-y-3 text-sm">
                    <thead>
                      <tr className="text-left text-slate-500">
                        <th className="px-4 py-2 font-medium">Лекарство</th>
                        <th className="px-4 py-2 font-medium">Категория</th>
                        <th className="px-4 py-2 font-medium">Статус</th>
                        <th className="px-4 py-2 font-medium">Склад</th>
                        <th className="px-4 py-2 font-medium">Barcode</th>
                      </tr>
                    </thead>
                    <tbody>
                      {medicines.data.items.map((medicine) => (
                        <tr
                          key={medicine.id}
                          className="cursor-pointer align-top"
                          onClick={() => setSelectedMedicine(medicine)}
                        >
                          <td className="rounded-l-[1.75rem] border-y border-l border-slate-200/70 bg-white/85 px-4 py-4">
                            <p className="font-semibold text-slate-950">{medicine.brandName}</p>
                            <p className="mt-1 text-xs text-slate-500">
                              {medicine.genericName} · {medicine.dosageForm} · {medicine.strength}
                            </p>
                            <p className="mt-2 text-xs text-slate-400">{medicine.manufacturer}</p>
                          </td>
                          <td className="border-y border-slate-200/70 bg-white/85 px-4 py-4 text-slate-700">
                            {medicine.categoryName || 'Без категории'}
                          </td>
                          <td className="border-y border-slate-200/70 bg-white/85 px-4 py-4">
                            <div className="flex flex-wrap gap-2">
                              <StatusBadge tone={getActiveStateTone(medicine.isActive)}>
                                {getActiveStateLabel(medicine.isActive)}
                              </StatusBadge>
                              {medicine.requiresPrescription ? (
                                <StatusBadge tone="warning">Rx</StatusBadge>
                              ) : (
                                <StatusBadge tone="neutral">OTC</StatusBadge>
                              )}
                            </div>
                          </td>
                          <td className="border-y border-slate-200/70 bg-white/85 px-4 py-4 text-slate-700">
                            <p>Партии: {formatNumber(medicine.stockBatchCount)}</p>
                            <p className="mt-1 text-xs text-slate-500">Поставщики: {formatNumber(medicine.supplierOfferCount)}</p>
                          </td>
                          <td className="rounded-r-[1.75rem] border-y border-r border-slate-200/70 bg-white/85 px-4 py-4 text-slate-700">
                            {medicine.barcode || 'Нет barcode'}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>

                <div className="mt-4 flex items-center justify-between rounded-[1.5rem] border border-slate-200/70 bg-white/80 px-4 py-3">
                  <p className="text-sm text-slate-500">
                    Страница {medicines.data.page} из {medicines.data.totalPages}
                  </p>
                  <div className="flex gap-2">
                    <Button variant="outline" size="sm" disabled={medicines.data.page <= 1} onClick={() => setPage((value) => Math.max(1, value - 1))}>
                      Назад
                    </Button>
                    <Button
                      variant="outline"
                      size="sm"
                      disabled={medicines.data.page >= medicines.data.totalPages}
                      onClick={() => setPage((value) => value + 1)}
                    >
                      Дальше
                    </Button>
                  </div>
                </div>
              </>
            ) : (
              <EmptyState
                title="Каталог пуст"
                description="Создай первое лекарство или расширь фильтр поиска, если работаешь на уже заполненной базе."
              />
            )}
          </CardContent>
        </Card>

        <div className="space-y-6">
          <Card className="admin-glass border-white/60 bg-white/95">
            <CardHeader className="flex flex-row items-start justify-between gap-4">
              <div className="space-y-1">
                <CardTitle>{selectedMedicine ? 'Редактирование лекарства' : 'Новая карточка лекарства'}</CardTitle>
                <p className="text-sm text-slate-500">Форма управляет теми полями, от которых зависят поиск, резервирование и inventory surfaces.</p>
              </div>
              {selectedMedicine ? (
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => {
                    setSelectedMedicine(null)
                    medicineForm.reset()
                  }}
                >
                  Сбросить выбор
                </Button>
              ) : null}
            </CardHeader>
            <CardContent>
              <form className="space-y-4" onSubmit={medicineForm.handleSubmit(submitMedicine)}>
                <div className="grid gap-4 md:grid-cols-2">
                  <FormField label="Бренд" error={medicineForm.formState.errors.brandName?.message}>
                    <Input {...medicineForm.register('brandName')} placeholder="Например Panadol" />
                  </FormField>
                  <FormField label="Дженерик" error={medicineForm.formState.errors.genericName?.message}>
                    <Input {...medicineForm.register('genericName')} placeholder="Paracetamol" />
                  </FormField>
                  <FormField label="Лекарственная форма" error={medicineForm.formState.errors.dosageForm?.message}>
                    <Input {...medicineForm.register('dosageForm')} placeholder="Tablet" />
                  </FormField>
                  <FormField label="Дозировка" error={medicineForm.formState.errors.strength?.message}>
                    <Input {...medicineForm.register('strength')} placeholder="500 mg" />
                  </FormField>
                  <FormField label="Производитель" error={medicineForm.formState.errors.manufacturer?.message}>
                    <Input {...medicineForm.register('manufacturer')} placeholder="GSK" />
                  </FormField>
                  <FormField label="Страна" error={medicineForm.formState.errors.countryOfOrigin?.message}>
                    <Input {...medicineForm.register('countryOfOrigin')} placeholder="UK" />
                  </FormField>
                  <FormField label="Barcode" error={medicineForm.formState.errors.barcode?.message}>
                    <Input {...medicineForm.register('barcode')} placeholder="4791234567890" />
                  </FormField>
                  <div className="space-y-2">
                    <label className="text-sm font-medium text-slate-700">Категория</label>
                    <select
                      className="h-11 w-full rounded-[1rem] border border-slate-200 bg-white px-4 text-sm text-slate-950 outline-none focus:border-teal-500"
                      {...medicineForm.register('categoryId')}
                    >
                      <option value="">Без категории</option>
                      {(categories.data?.items ?? []).map((category) => (
                        <option key={category.id} value={category.id}>
                          {category.name}
                        </option>
                      ))}
                    </select>
                  </div>
                </div>

                <FormField label="Описание" error={medicineForm.formState.errors.description?.message}>
                  <textarea
                    className="min-h-28 w-full rounded-[1rem] border border-slate-200 bg-white px-4 py-3 text-sm text-slate-950 outline-none focus:border-teal-500"
                    {...medicineForm.register('description')}
                    placeholder="Короткое описание для каталога"
                  />
                </FormField>

                <div className="grid gap-3 md:grid-cols-2">
                  <label className="flex items-start gap-3 rounded-[1.25rem] border border-slate-200/70 bg-white/80 px-4 py-3 text-sm text-slate-700">
                    <input type="checkbox" className="mt-1 h-4 w-4 rounded border-slate-300" {...medicineForm.register('requiresPrescription')} />
                    <span>
                      <span className="font-medium text-slate-950">Требует рецепт</span>
                      <span className="mt-1 block text-slate-500">Эта настройка влияет на consumer и pharmacy workflows.</span>
                    </span>
                  </label>
                  <label className="flex items-start gap-3 rounded-[1.25rem] border border-slate-200/70 bg-white/80 px-4 py-3 text-sm text-slate-700">
                    <input type="checkbox" className="mt-1 h-4 w-4 rounded border-slate-300" {...medicineForm.register('isActive')} />
                    <span>
                      <span className="font-medium text-slate-950">Активная карточка</span>
                      <span className="mt-1 block text-slate-500">Неактивные записи остаются в истории, но скрываются из активной работы.</span>
                    </span>
                  </label>
                </div>

                {medicineError ? (
                  <div className="rounded-[1.25rem] border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
                    {medicineError}
                  </div>
                ) : null}

                <Button type="submit" disabled={isSubmitting}>
                  {isSubmitting ? 'Сохраняем...' : selectedMedicine ? 'Сохранить лекарство' : 'Создать лекарство'}
                </Button>
              </form>
            </CardContent>
          </Card>

          <Card className="admin-glass border-white/60 bg-white/95">
            <CardHeader className="flex flex-col gap-4">
              <div className="flex flex-wrap items-start justify-between gap-4">
                <div className="space-y-1">
                  <CardTitle>Категории</CardTitle>
                  <p className="text-sm text-slate-500">Категории помогают держать каталог чистым и удобным для поиска в остальных порталах.</p>
                </div>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => {
                    setSelectedCategory(null)
                    categoryForm.reset()
                    setCategoryFilter('')
                  }}
                >
                  Новая категория
                </Button>
              </div>
              <Input
                value={categorySearch}
                onChange={(event) => setCategorySearch(event.target.value)}
                placeholder="Поиск категорий"
              />
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="space-y-3">
                {categories.isLoading ? (
                  Array.from({ length: 4 }).map((_, index) => (
                    <div key={index} className="h-16 animate-pulse rounded-[1.25rem] bg-slate-100" />
                  ))
                ) : categories.data?.items?.length ? (
                  <>
                    <button
                      type="button"
                      className={`w-full rounded-[1.25rem] border px-4 py-3 text-left transition ${
                        categoryFilter === ''
                          ? 'border-teal-500 bg-teal-50'
                          : 'border-slate-200/70 bg-white/80 hover:border-teal-300'
                      }`}
                      onClick={() => setCategoryFilter('')}
                    >
                      <p className="font-medium text-slate-950">Все категории</p>
                      <p className="mt-1 text-sm text-slate-500">Показывать весь каталог без category-filter.</p>
                    </button>
                    {categories.data.items.map((category) => (
                      <button
                        key={category.id}
                        type="button"
                        className={`w-full rounded-[1.25rem] border px-4 py-3 text-left transition ${
                          categoryFilter === category.id
                            ? 'border-teal-500 bg-teal-50'
                            : 'border-slate-200/70 bg-white/80 hover:border-teal-300'
                        }`}
                        onClick={() => {
                          setSelectedCategory(category)
                          setCategoryFilter(category.id)
                        }}
                      >
                        <div className="flex items-center justify-between gap-3">
                          <p className="font-medium text-slate-950">{category.name}</p>
                          <StatusBadge tone="neutral">{formatNumber(category.medicinesCount)}</StatusBadge>
                        </div>
                        <p className="mt-1 text-sm text-slate-500">
                          {category.description || 'Без описания'}
                        </p>
                      </button>
                    ))}
                  </>
                ) : (
                  <EmptyState title="Категорий пока нет" description="Создай первую категорию для наведения порядка в каталоге." />
                )}
              </div>

              <form className="space-y-4 rounded-[1.5rem] border border-slate-200/70 bg-white/70 p-4" onSubmit={categoryForm.handleSubmit(submitCategory)}>
                <p className="font-medium text-slate-950">
                  {selectedCategory ? 'Редактирование категории' : 'Новая категория'}
                </p>
                <FormField label="Название" error={categoryForm.formState.errors.name?.message}>
                  <Input {...categoryForm.register('name')} placeholder="Например Pain relief" />
                </FormField>
                <FormField label="Описание" error={categoryForm.formState.errors.description?.message}>
                  <textarea
                    className="min-h-24 w-full rounded-[1rem] border border-slate-200 bg-white px-4 py-3 text-sm text-slate-950 outline-none focus:border-teal-500"
                    {...categoryForm.register('description')}
                    placeholder="Короткое описание категории"
                  />
                </FormField>

                {categoryError ? (
                  <div className="rounded-[1.25rem] border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
                    {categoryError}
                  </div>
                ) : null}

                <Button type="submit" disabled={isSubmitting}>
                  {isSubmitting ? 'Сохраняем...' : selectedCategory ? 'Сохранить категорию' : 'Создать категорию'}
                </Button>
              </form>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  )
}

function FormField({
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
