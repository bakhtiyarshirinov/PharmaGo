'use client'

import * as Dialog from '@radix-ui/react-dialog'
import { useEffect, useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Check, Search, X } from 'lucide-react'
import type { StockItem } from '@pharmago/types'
import { Button, Input } from '@pharmago/ui'
import { useCreateStockItem, useMedicineSuggestions, useUpdateStockItem } from '../hooks'
import { stockEditorSchema, type StockEditorValues } from '../schemas'

interface StockEditorDialogProps {
  pharmacyId: string
  stockItem?: StockItem
  triggerLabel: string
  triggerVariant?: 'primary' | 'secondary' | 'ghost' | 'outline' | 'destructive'
}

function toDateInputValue(value?: string | null) {
  if (!value) {
    return ''
  }

  return value.slice(0, 10)
}

export function StockEditorDialog({
  pharmacyId,
  stockItem,
  triggerLabel,
  triggerVariant = 'outline',
}: StockEditorDialogProps) {
  const [open, setOpen] = useState(false)
  const [medicineQuery, setMedicineQuery] = useState(stockItem?.medicineName ?? '')
  const isEdit = Boolean(stockItem)
  const createStock = useCreateStockItem()
  const updateStock = useUpdateStockItem(stockItem?.id ?? stockItem?.stockItemId ?? '')
  const suggestions = useMedicineSuggestions(medicineQuery)

  const form = useForm<StockEditorValues>({
    resolver: zodResolver(stockEditorSchema),
    defaultValues: {
      medicineId: stockItem?.medicineId ?? '',
      batchNumber: stockItem?.batchNumber ?? '',
      expirationDate: toDateInputValue(stockItem?.expirationDate),
      quantity: stockItem?.quantity ?? 0,
      purchasePrice: stockItem?.purchasePrice ?? 0,
      retailPrice: stockItem?.retailPrice ?? 0,
      reorderLevel: stockItem?.reorderLevel ?? 0,
      isActive: stockItem?.isActive ?? true,
    },
  })

  useEffect(() => {
    if (!open) {
      form.reset({
        medicineId: stockItem?.medicineId ?? '',
        batchNumber: stockItem?.batchNumber ?? '',
        expirationDate: toDateInputValue(stockItem?.expirationDate),
        quantity: stockItem?.quantity ?? 0,
        purchasePrice: stockItem?.purchasePrice ?? 0,
        retailPrice: stockItem?.retailPrice ?? 0,
        reorderLevel: stockItem?.reorderLevel ?? 0,
        isActive: stockItem?.isActive ?? true,
      })
      setMedicineQuery(stockItem?.medicineName ?? '')
    }
  }, [form, open, stockItem])

  const selectedMedicineLabel = useMemo(() => {
    if (isEdit) {
      return stockItem?.medicineName ?? ''
    }

    if (!form.watch('medicineId')) {
      return ''
    }

    const match = suggestions.data?.find((item) => item.medicineId === form.getValues('medicineId'))
    return match ? `${match.brandName} · ${match.genericName}` : medicineQuery
  }, [form, isEdit, medicineQuery, stockItem?.medicineName, suggestions.data])

  async function onSubmit(values: StockEditorValues) {
    if (isEdit) {
      await updateStock.mutateAsync({
        batchNumber: values.batchNumber,
        expirationDate: values.expirationDate,
        quantity: values.quantity,
        purchasePrice: values.purchasePrice,
        retailPrice: values.retailPrice,
        reorderLevel: values.reorderLevel,
        isActive: values.isActive,
      })
    } else {
      await createStock.mutateAsync({
        pharmacyId,
        medicineId: values.medicineId,
        batchNumber: values.batchNumber,
        expirationDate: values.expirationDate,
        quantity: values.quantity,
        purchasePrice: values.purchasePrice,
        retailPrice: values.retailPrice,
        reorderLevel: values.reorderLevel,
      })
    }

    setOpen(false)
  }

  const isSubmitting = createStock.isPending || updateStock.isPending

  return (
    <Dialog.Root open={open} onOpenChange={setOpen}>
      <Dialog.Trigger asChild>
        <Button variant={triggerVariant}>{triggerLabel}</Button>
      </Dialog.Trigger>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 bg-slate-950/60" />
        <Dialog.Content className="fixed inset-y-4 right-4 w-full max-w-2xl overflow-y-auto rounded-[2rem] bg-white p-6 shadow-2xl">
          <div className="flex items-start justify-between gap-4">
            <div>
              <Dialog.Title className="text-2xl font-semibold text-slate-950">
                {isEdit ? 'Редактирование партии' : 'Новая партия на склад'}
              </Dialog.Title>
              <Dialog.Description className="mt-1 text-sm text-slate-500">
                {isEdit
                  ? 'Обновите параметры партии, если изменились цены, остаток или активность записи.'
                  : 'Добавьте новую складскую партию в текущую аптеку.'}
              </Dialog.Description>
            </div>
            <Dialog.Close asChild>
              <button className="rounded-full p-2 text-slate-500 hover:bg-slate-100" type="button">
                <X className="h-4 w-4" />
              </button>
            </Dialog.Close>
          </div>

          <form className="mt-6 space-y-5" onSubmit={form.handleSubmit(onSubmit)}>
            <div className="space-y-2">
              <label className="text-sm font-medium text-slate-700">Лекарство</label>
              {isEdit ? (
                <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-700">
                  {selectedMedicineLabel}
                </div>
              ) : (
                <>
                  <div className="relative">
                    <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
                    <Input
                      className="pl-10"
                      placeholder="Начните вводить название лекарства"
                      value={medicineQuery}
                      onChange={(event) => setMedicineQuery(event.target.value)}
                    />
                  </div>
                  <input type="hidden" {...form.register('medicineId')} />
                  {medicineQuery.trim().length >= 2 ? (
                    <div className="max-h-48 overflow-y-auto rounded-2xl border border-slate-200">
                      {suggestions.data?.length ? (
                        suggestions.data.map((medicine) => {
                          const isSelected = form.watch('medicineId') === medicine.medicineId

                          return (
                            <button
                              key={medicine.medicineId}
                              className="flex w-full items-center justify-between border-b border-slate-100 px-4 py-3 text-left last:border-b-0 hover:bg-slate-50"
                              type="button"
                              onClick={() => {
                                form.setValue('medicineId', medicine.medicineId, { shouldValidate: true })
                                setMedicineQuery(`${medicine.brandName} · ${medicine.genericName}`)
                              }}
                            >
                              <div>
                                <p className="font-medium text-slate-950">{medicine.brandName}</p>
                                <p className="text-sm text-slate-500">{medicine.genericName}</p>
                              </div>
                              {isSelected ? <Check className="h-4 w-4 text-emerald-600" /> : null}
                            </button>
                          )
                        })
                      ) : suggestions.isFetching ? (
                        <div className="px-4 py-3 text-sm text-slate-500">Ищем лекарства...</div>
                      ) : (
                        <div className="px-4 py-3 text-sm text-slate-500">Подсказки не найдены.</div>
                      )}
                    </div>
                  ) : (
                    <p className="text-sm text-slate-500">Введите минимум 2 символа, чтобы выбрать лекарство.</p>
                  )}
                </>
              )}
              {form.formState.errors.medicineId ? (
                <p className="text-sm text-red-600">{form.formState.errors.medicineId.message}</p>
              ) : null}
            </div>

            <div className="grid gap-4 md:grid-cols-2">
              <Field label="Номер партии" error={form.formState.errors.batchNumber?.message}>
                <Input {...form.register('batchNumber')} placeholder="Например BATCH-102" />
              </Field>
              <Field label="Срок годности" error={form.formState.errors.expirationDate?.message}>
                <Input type="date" {...form.register('expirationDate')} />
              </Field>
              <Field label="Количество" error={form.formState.errors.quantity?.message}>
                <Input type="number" {...form.register('quantity')} />
              </Field>
              <Field label="Точка дозакупки" error={form.formState.errors.reorderLevel?.message}>
                <Input type="number" {...form.register('reorderLevel')} />
              </Field>
              <Field label="Закупочная цена" error={form.formState.errors.purchasePrice?.message}>
                <Input type="number" step="0.01" {...form.register('purchasePrice')} />
              </Field>
              <Field label="Розничная цена" error={form.formState.errors.retailPrice?.message}>
                <Input type="number" step="0.01" {...form.register('retailPrice')} />
              </Field>
            </div>

            {isEdit ? (
              <label className="flex items-center gap-3 rounded-2xl bg-slate-50 px-4 py-3 text-sm text-slate-700">
                <input type="checkbox" className="h-4 w-4 rounded border-slate-300" {...form.register('isActive')} />
                Активная партия
              </label>
            ) : null}

            <div className="flex flex-wrap justify-end gap-3">
              <Dialog.Close asChild>
                <Button type="button" variant="ghost">
                  Отмена
                </Button>
              </Dialog.Close>
              <Button type="submit" disabled={isSubmitting}>
                {isSubmitting ? 'Сохраняем...' : isEdit ? 'Сохранить изменения' : 'Создать партию'}
              </Button>
            </div>
          </form>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
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
