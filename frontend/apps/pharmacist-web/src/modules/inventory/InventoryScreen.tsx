'use client'

import { useMemo, useState } from 'react'
import { mapBackendRole, useAuthStore } from '@pharmago/auth/client'
import type { AuthSession } from '@pharmago/types'
import { Button, Card, CardContent, CardHeader, CardTitle, EmptyState, Input, PageHeader, StatusBadge } from '@pharmago/ui'
import { formatDate, formatDateTime, formatMoney, formatNumber } from '../../lib/format'
import {
  useExpiringAlerts,
  useInventoryStock,
  useLowStockAlerts,
  useOutOfStockAlerts,
  useRestockSuggestions,
} from './hooks'
import { StockEditorDialog } from './components/StockEditorDialog'
import { StockQuantityActionDialog } from './components/StockQuantityActionDialog'

interface InventoryScreenProps {
  initialSession?: AuthSession | null
}

export function InventoryScreen({ initialSession = null }: InventoryScreenProps) {
  const liveSession = useAuthStore((state) => state.session)
  const session = liveSession ?? initialSession
  const pharmacyId = session?.user.pharmacyId ?? null
  const role = session?.user.role ? mapBackendRole(session.user.role) : 'guest'
  const canManageInventory = role === 'pharmacist' || role === 'admin'
  const [search, setSearch] = useState('')
  const [lowStockOnly, setLowStockOnly] = useState(false)
  const [expiringDays, setExpiringDays] = useState(21)

  const stock = useInventoryStock(pharmacyId, lowStockOnly)
  const lowStock = useLowStockAlerts(pharmacyId)
  const outOfStock = useOutOfStockAlerts(pharmacyId)
  const expiring = useExpiringAlerts(pharmacyId, expiringDays)
  const restock = useRestockSuggestions(pharmacyId)

  const filteredStock = useMemo(() => {
    const query = search.trim().toLowerCase()

    return (stock.data ?? []).filter((item) => {
      if (!query) {
        return true
      }

      const haystack = [item.medicineName, item.genericName, item.batchNumber].join(' ').toLowerCase()
      return haystack.includes(query)
    })
  }, [search, stock.data])

  if (!canManageInventory) {
    return (
      <EmptyState
        title="Нет доступа к складу"
        description="Для этой страницы требуется право управления остатками."
      />
    )
  }

  if (!pharmacyId) {
    return (
      <EmptyState
        title="Нет привязки к аптеке"
        description="У сотрудника нет назначенной аптеки, поэтому складской контур недоступен."
      />
    )
  }

  return (
    <div className="space-y-8">
      <PageHeader
        eyebrow="Склад и алерты"
        title="Остатки, просадка, приход и списание в одном рабочем экране."
        description="Сначала список партий по вашей аптеке, затем low-stock, out-of-stock, expiring и рекомендации на дозакупку."
        actions={<StockEditorDialog pharmacyId={pharmacyId} triggerLabel="Добавить партию" triggerVariant="primary" />}
      />

      <div className="grid gap-6 xl:grid-cols-[1.15fr,0.85fr]">
        <Card className="ops-glass border-white/10 bg-white/95">
          <CardHeader className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
            <div className="space-y-1">
              <CardTitle>Партии на складе</CardTitle>
              <p className="text-sm text-slate-500">Таблица ориентирована на быстрые корректировки и операционные действия по партии.</p>
            </div>
            <div className="flex w-full flex-col gap-3 sm:flex-row lg:max-w-xl">
              <Input
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                placeholder="Поиск по лекарству, дженерику или партии"
              />
              <Button variant={lowStockOnly ? 'primary' : 'outline'} onClick={() => setLowStockOnly((value) => !value)}>
                {lowStockOnly ? 'Показываем только low-stock' : 'Все партии'}
              </Button>
            </div>
          </CardHeader>
          <CardContent>
            {stock.isLoading ? (
              <div className="space-y-3">
                {Array.from({ length: 6 }).map((_, index) => (
                  <div key={index} className="h-20 animate-pulse rounded-3xl bg-slate-100" />
                ))}
              </div>
            ) : stock.isError ? (
              <div className="rounded-3xl border border-red-200 bg-red-50 p-5 text-sm text-red-700">
                Не удалось загрузить список партий.
              </div>
            ) : filteredStock.length === 0 ? (
              <EmptyState title="Партии не найдены" description="Измените поиск или снимите фильтр low-stock." />
            ) : (
              <div className="overflow-x-auto">
                <table className="min-w-full border-separate border-spacing-y-3 text-sm">
                  <thead>
                    <tr className="text-left text-slate-500">
                      <th className="px-4 py-2 font-medium">Лекарство</th>
                      <th className="px-4 py-2 font-medium">Партия</th>
                      <th className="px-4 py-2 font-medium">Остаток</th>
                      <th className="px-4 py-2 font-medium">Срок</th>
                      <th className="px-4 py-2 font-medium">Цены</th>
                      <th className="px-4 py-2 font-medium">Действия</th>
                    </tr>
                  </thead>
                  <tbody>
                    {filteredStock.map((item) => (
                      <tr key={item.id ?? item.stockItemId} className="align-top">
                        <td className="rounded-l-[1.75rem] border-y border-l border-slate-200/70 bg-white/85 px-4 py-4">
                          <div className="space-y-1">
                            <p className="font-medium text-slate-950">{item.medicineName}</p>
                            <p className="text-xs text-slate-500">{item.genericName || 'Без дженерика'}</p>
                            {item.isLowStock ? <StatusBadge tone="warning">Low stock</StatusBadge> : null}
                          </div>
                        </td>
                        <td className="border-y border-slate-200/70 bg-white/85 px-4 py-4">
                          <p className="font-medium text-slate-900">{item.batchNumber}</p>
                          <p className="mt-1 text-xs text-slate-500">{item.isActive === false ? 'Неактивна' : 'Активна'}</p>
                        </td>
                        <td className="border-y border-slate-200/70 bg-white/85 px-4 py-4">
                          <p className="font-medium text-slate-900">{formatNumber(item.availableQuantity)}</p>
                          <p className="mt-1 text-xs text-slate-500">
                            Всего {formatNumber(item.quantity)} · зарезервировано {formatNumber(item.reservedQuantity)}
                          </p>
                        </td>
                        <td className="border-y border-slate-200/70 bg-white/85 px-4 py-4">
                          <p className="font-medium text-slate-900">{formatDate(item.expirationDate)}</p>
                          <p className="mt-1 text-xs text-slate-500">
                            Обновлено {formatDateTime(item.lastStockUpdatedAtUtc ?? item.expirationDate)}
                          </p>
                        </td>
                        <td className="border-y border-slate-200/70 bg-white/85 px-4 py-4">
                          <p className="font-medium text-slate-900">{formatMoney(item.retailPrice)}</p>
                          <p className="mt-1 text-xs text-slate-500">Закупка {formatMoney(item.purchasePrice)}</p>
                        </td>
                        <td className="rounded-r-[1.75rem] border-y border-r border-slate-200/70 bg-white/85 px-4 py-4">
                          <div className="flex flex-wrap gap-2">
                            <StockEditorDialog pharmacyId={pharmacyId} stockItem={item} triggerLabel="Редактировать" />
                            <StockQuantityActionDialog action="adjust" stockItem={item} triggerLabel="Коррект." />
                            <StockQuantityActionDialog action="receive" stockItem={item} triggerLabel="Приход" />
                            <StockQuantityActionDialog action="writeoff" stockItem={item} triggerLabel="Списать" />
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </CardContent>
        </Card>

        <div className="space-y-6">
          <Card className="ops-glass border-white/10 bg-white/95">
            <CardHeader>
              <CardTitle>Low-stock snapshot</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              {lowStock.data?.length ? (
                lowStock.data.slice(0, 4).map((item) => (
                  <div
                    key={item.stockItemId}
                    className="rounded-[1.75rem] border border-slate-200/80 bg-white/85 p-4 shadow-[0_22px_60px_rgba(15,23,42,0.08)]"
                  >
                    <p className="font-medium text-slate-950">{item.medicineName}</p>
                    <p className="mt-1 text-sm text-slate-500">
                      Доступно {formatNumber(item.availableQuantity)} · точка {formatNumber(item.reorderLevel)}
                    </p>
                  </div>
                ))
              ) : (
                <EmptyState title="Low-stock нет" description="Критических просадок по точке дозакупки сейчас нет." />
              )}
            </CardContent>
          </Card>

          <Card className="ops-glass border-white/10 bg-white/95">
            <CardHeader>
              <CardTitle>Out-of-stock snapshot</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              {outOfStock.data?.length ? (
                outOfStock.data.slice(0, 4).map((item) => (
                  <div
                    key={`${item.pharmacyId}-${item.medicineId}`}
                    className="rounded-[1.75rem] border border-slate-200/80 bg-white/85 p-4 shadow-[0_22px_60px_rgba(15,23,42,0.08)]"
                  >
                    <p className="font-medium text-slate-950">{item.medicineName}</p>
                    <p className="mt-1 text-sm text-slate-500">
                      Пакетов: {formatNumber(item.batchCount)} · точка {formatNumber(item.reorderLevel)}
                    </p>
                  </div>
                ))
              ) : (
                <EmptyState title="Out-of-stock нет" description="Полностью закончившихся позиций сейчас нет." />
              )}
            </CardContent>
          </Card>

          <Card className="ops-glass border-white/10 bg-white/95">
            <CardHeader className="flex flex-row items-start justify-between gap-4">
              <div>
                <CardTitle>Скоро истекают</CardTitle>
              </div>
              <div className="w-28">
                <Input
                  type="number"
                  min={1}
                  max={180}
                  value={expiringDays}
                  onChange={(event) => setExpiringDays(Number(event.target.value) || 21)}
                />
              </div>
            </CardHeader>
            <CardContent className="space-y-3">
              {expiring.data?.length ? (
                expiring.data.slice(0, 4).map((item) => (
                  <div
                    key={item.stockItemId}
                    className="rounded-[1.75rem] border border-slate-200/80 bg-white/85 p-4 shadow-[0_22px_60px_rgba(15,23,42,0.08)]"
                  >
                    <p className="font-medium text-slate-950">{item.medicineName}</p>
                    <p className="mt-1 text-sm text-slate-500">
                      Осталось дней: {formatNumber(item.daysUntilExpiration)} · доступно {formatNumber(item.availableQuantity)}
                    </p>
                  </div>
                ))
              ) : (
                <EmptyState title="Истекающих партий нет" description="В выбранном окне все партии выглядят безопасно." />
              )}
            </CardContent>
          </Card>

          <Card className="ops-glass border-white/10 bg-white/95">
            <CardHeader>
              <CardTitle>Рекомендации на дозакупку</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              {restock.data?.length ? (
                restock.data.slice(0, 4).map((item) => (
                  <div
                    key={item.stockItemId}
                    className="rounded-[1.75rem] border border-slate-200/80 bg-white/85 p-4 shadow-[0_22px_60px_rgba(15,23,42,0.08)]"
                  >
                    <p className="font-medium text-slate-950">{item.medicineName}</p>
                    <p className="mt-1 text-sm text-slate-500">
                      Заказать {formatNumber(item.suggestedOrderQuantity)} у {item.depotName}
                    </p>
                    <p className="mt-1 text-sm text-slate-500">Оценка: {formatMoney(item.estimatedWholesaleCost)}</p>
                  </div>
                ))
              ) : (
                <EmptyState title="Дозакупка не требуется" description="Backend не вернул рекомендаций на текущий момент." />
              )}
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  )
}
