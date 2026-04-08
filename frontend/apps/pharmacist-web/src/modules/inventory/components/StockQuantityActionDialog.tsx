'use client'

import * as Dialog from '@radix-ui/react-dialog'
import { useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { X } from 'lucide-react'
import type { StockItem } from '@pharmago/types'
import { Button, Input } from '@pharmago/ui'
import { formatNumber } from '../../../lib/format'
import {
  useAdjustStockItem,
  useReceiveStockItem,
  useWriteOffStockItem,
} from '../hooks'
import {
  stockAdjustSchema,
  stockReceiveSchema,
  stockWriteoffSchema,
  type StockAdjustValues,
  type StockReceiveValues,
  type StockWriteoffValues,
} from '../schemas'

type ActionType = 'adjust' | 'receive' | 'writeoff'

interface StockQuantityActionDialogProps {
  action: ActionType
  stockItem: StockItem
  triggerLabel: string
}

function normalizeOptionalNumber(value: number | undefined) {
  return Number.isFinite(value) ? value : undefined
}

export function StockQuantityActionDialog({
  action,
  stockItem,
  triggerLabel,
}: StockQuantityActionDialogProps) {
  const [open, setOpen] = useState(false)
  const stockItemId = stockItem.id ?? stockItem.stockItemId ?? ''
  const adjust = useAdjustStockItem(stockItemId)
  const receive = useReceiveStockItem(stockItemId)
  const writeoff = useWriteOffStockItem(stockItemId)

  const adjustForm = useForm<StockAdjustValues>({
    resolver: zodResolver(stockAdjustSchema),
    defaultValues: {
      quantityDelta: 0,
      reason: '',
    },
  })

  const receiveForm = useForm<StockReceiveValues>({
    resolver: zodResolver(stockReceiveSchema),
    defaultValues: {
      quantityReceived: 1,
      purchasePrice: undefined,
      retailPrice: undefined,
      reorderLevel: undefined,
      reason: '',
    },
  })

  const writeoffForm = useForm<StockWriteoffValues>({
    resolver: zodResolver(stockWriteoffSchema),
    defaultValues: {
      quantity: 1,
      reason: '',
    },
  })

  const descriptor = useMemo(() => {
    if (action === 'adjust') {
      return {
        title: 'Корректировка остатка',
        description: 'Используйте для сверки после инвентаризации, пересчета или исправления ошибки.',
      }
    }

    if (action === 'receive') {
      return {
        title: 'Поступление',
        description: 'Приход новой поставки в существующую партию с возможностью обновить цены и точку дозакупки.',
      }
    }

    return {
      title: 'Списание',
      description: 'Списание из доступного остатка с обязательной причиной.',
    }
  }, [action])

  const isSubmitting = adjust.isPending || receive.isPending || writeoff.isPending

  async function submitAdjust(values: StockAdjustValues) {
    await adjust.mutateAsync(values)
    setOpen(false)
    adjustForm.reset({ quantityDelta: 0, reason: '' })
  }

  async function submitReceive(values: StockReceiveValues) {
    await receive.mutateAsync({
      quantityReceived: values.quantityReceived,
      purchasePrice: normalizeOptionalNumber(values.purchasePrice),
      retailPrice: normalizeOptionalNumber(values.retailPrice),
      reorderLevel: normalizeOptionalNumber(values.reorderLevel),
      reason: values.reason?.trim() ? values.reason.trim() : undefined,
    })
    setOpen(false)
    receiveForm.reset({
      quantityReceived: 1,
      purchasePrice: undefined,
      retailPrice: undefined,
      reorderLevel: undefined,
      reason: '',
    })
  }

  async function submitWriteoff(values: StockWriteoffValues) {
    await writeoff.mutateAsync(values)
    setOpen(false)
    writeoffForm.reset({ quantity: 1, reason: '' })
  }

  return (
    <Dialog.Root open={open} onOpenChange={setOpen}>
      <Dialog.Trigger asChild>
        <Button variant="outline" size="sm">
          {triggerLabel}
        </Button>
      </Dialog.Trigger>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 bg-slate-950/60" />
        <Dialog.Content className="fixed inset-y-10 right-4 w-full max-w-xl overflow-y-auto rounded-[2rem] bg-white p-6 shadow-2xl">
          <div className="flex items-start justify-between gap-4">
            <div>
              <Dialog.Title className="text-2xl font-semibold text-slate-950">{descriptor.title}</Dialog.Title>
              <Dialog.Description className="mt-1 text-sm text-slate-500">{descriptor.description}</Dialog.Description>
            </div>
            <Dialog.Close asChild>
              <button className="rounded-full p-2 text-slate-500 hover:bg-slate-100" type="button">
                <X className="h-4 w-4" />
              </button>
            </Dialog.Close>
          </div>

          <div className="mt-5 rounded-3xl bg-slate-50 p-4 text-sm text-slate-700">
            <p className="font-medium text-slate-950">{stockItem.medicineName}</p>
            <p className="mt-1">Партия: {stockItem.batchNumber}</p>
            <p className="mt-1">Доступно: {formatNumber(stockItem.availableQuantity)}</p>
          </div>

          {action === 'adjust' ? (
            <form className="mt-6 space-y-5" onSubmit={adjustForm.handleSubmit(submitAdjust)}>
              <Field label="Изменение количества" error={adjustForm.formState.errors.quantityDelta?.message}>
                <Input type="number" {...adjustForm.register('quantityDelta')} />
              </Field>
              <Field label="Причина" error={adjustForm.formState.errors.reason?.message}>
                <textarea
                  className="min-h-28 w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm text-slate-950 outline-none focus:border-emerald-500"
                  {...adjustForm.register('reason')}
                  placeholder="Например: пересчет смены, ошибка приемки, инвентаризация"
                />
              </Field>
              <DialogFooter disabled={isSubmitting} />
            </form>
          ) : null}

          {action === 'receive' ? (
            <form className="mt-6 space-y-5" onSubmit={receiveForm.handleSubmit(submitReceive)}>
              <div className="grid gap-4 md:grid-cols-2">
                <Field label="Количество прихода" error={receiveForm.formState.errors.quantityReceived?.message}>
                  <Input type="number" {...receiveForm.register('quantityReceived')} />
                </Field>
                <Field label="Новая точка дозакупки" error={receiveForm.formState.errors.reorderLevel?.message}>
                  <Input type="number" {...receiveForm.register('reorderLevel')} />
                </Field>
                <Field label="Новая закупочная цена" error={receiveForm.formState.errors.purchasePrice?.message}>
                  <Input type="number" step="0.01" {...receiveForm.register('purchasePrice')} />
                </Field>
                <Field label="Новая розничная цена" error={receiveForm.formState.errors.retailPrice?.message}>
                  <Input type="number" step="0.01" {...receiveForm.register('retailPrice')} />
                </Field>
              </div>
              <Field label="Комментарий" error={receiveForm.formState.errors.reason?.message}>
                <textarea
                  className="min-h-28 w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm text-slate-950 outline-none focus:border-emerald-500"
                  {...receiveForm.register('reason')}
                  placeholder="Необязательный комментарий по приходу"
                />
              </Field>
              <DialogFooter disabled={isSubmitting} />
            </form>
          ) : null}

          {action === 'writeoff' ? (
            <form className="mt-6 space-y-5" onSubmit={writeoffForm.handleSubmit(submitWriteoff)}>
              <Field label="Количество к списанию" error={writeoffForm.formState.errors.quantity?.message}>
                <Input type="number" {...writeoffForm.register('quantity')} />
              </Field>
              <Field label="Причина списания" error={writeoffForm.formState.errors.reason?.message}>
                <textarea
                  className="min-h-28 w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm text-slate-950 outline-none focus:border-emerald-500"
                  {...writeoffForm.register('reason')}
                  placeholder="Например: повреждение упаковки, истечение срока, возврат"
                />
              </Field>
              <DialogFooter disabled={isSubmitting} destructive />
            </form>
          ) : null}
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

function DialogFooter({ disabled, destructive = false }: { disabled: boolean; destructive?: boolean }) {
  return (
    <div className="flex flex-wrap justify-end gap-3">
      <Dialog.Close asChild>
        <Button type="button" variant="ghost">
          Отмена
        </Button>
      </Dialog.Close>
      <Button type="submit" variant={destructive ? 'destructive' : 'primary'} disabled={disabled}>
        {disabled ? 'Сохраняем...' : 'Подтвердить'}
      </Button>
    </div>
  )
}
