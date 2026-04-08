'use client'

import { useState, useTransition } from 'react'
import { useRouter } from 'next/navigation'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { Button, Card, CardContent, CardDescription, CardHeader, CardTitle, Input, StatusBadge } from '@pharmago/ui'
import { useAuth } from '@pharmago/auth/client'
import { getApiErrorMessage } from '../../../lib/errors'

const pharmacistLoginSchema = z.object({
  phoneNumber: z
    .string()
    .min(10, 'Укажите номер телефона.')
    .max(32, 'Номер телефона слишком длинный.'),
  password: z
    .string()
    .min(8, 'Пароль должен содержать не менее 8 символов.')
    .max(128, 'Пароль слишком длинный.'),
})

type PharmacistLoginValues = z.infer<typeof pharmacistLoginSchema>

export function PharmacistLoginForm() {
  const router = useRouter()
  const { login } = useAuth()
  const [serverError, setServerError] = useState<string | null>(null)
  const [isRedirecting, startTransition] = useTransition()
  const form = useForm<PharmacistLoginValues>({
    resolver: zodResolver(pharmacistLoginSchema),
    defaultValues: {
      phoneNumber: '+994500000001',
      password: 'Pharmacist123!',
    },
  })

  const isSubmitting = form.formState.isSubmitting || isRedirecting

  async function onSubmit(values: PharmacistLoginValues) {
    setServerError(null)

    try {
      await login(values)
      startTransition(() => {
        router.replace('/cockpit')
      })
    } catch (error) {
      setServerError(getApiErrorMessage(error, 'Не удалось открыть рабочее место фармацевта.'))
    }
  }

  return (
    <Card className="ops-glass max-w-lg rounded-[2rem] border-0 bg-white/[0.96] shadow-2xl shadow-slate-950/20">
      <CardHeader className="space-y-3">
        <StatusBadge tone="info">Только для сотрудников аптеки</StatusBadge>
        <CardTitle className="ops-display text-4xl text-slate-950">Вход в рабочую смену</CardTitle>
        <CardDescription className="text-sm leading-6 text-slate-600">
          После входа откроется очередь резервов, уведомления и управление остатками по вашей аптеке.
        </CardDescription>
      </CardHeader>
      <CardContent>
        <form className="space-y-5" onSubmit={form.handleSubmit(onSubmit)}>
          <div className="space-y-2">
            <label className="text-sm font-medium text-slate-700" htmlFor="phoneNumber">
              Телефон
            </label>
            <Input
              id="phoneNumber"
              autoComplete="tel"
              placeholder="+994500000001"
              {...form.register('phoneNumber')}
            />
            {form.formState.errors.phoneNumber ? (
              <p className="text-sm text-red-600">{form.formState.errors.phoneNumber.message}</p>
            ) : null}
          </div>

          <div className="space-y-2">
            <label className="text-sm font-medium text-slate-700" htmlFor="password">
              Пароль
            </label>
            <Input
              id="password"
              type="password"
              autoComplete="current-password"
              placeholder="Введите пароль"
              {...form.register('password')}
            />
            {form.formState.errors.password ? (
              <p className="text-sm text-red-600">{form.formState.errors.password.message}</p>
            ) : null}
          </div>

          {serverError ? (
            <div className="rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
              {serverError}
            </div>
          ) : null}

          <Button className="w-full rounded-full" type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Открываем рабочее место...' : 'Войти'}
          </Button>
        </form>
      </CardContent>
    </Card>
  )
}
