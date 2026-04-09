'use client'

import { useState, useTransition } from 'react'
import { useRouter } from 'next/navigation'
import { useAuth } from '@pharmago/auth/client'
import { Button, Card, CardContent, CardDescription, CardHeader, CardTitle, Input, StatusBadge } from '@pharmago/ui'
import { getApiErrorMessage } from '../../../lib/errors'

export function AdminLoginForm() {
  const router = useRouter()
  const { login } = useAuth()
  const [phoneNumber, setPhoneNumber] = useState('+994509990003')
  const [password, setPassword] = useState('Moderator123!')
  const [serverError, setServerError] = useState<string | null>(null)
  const [isPending, startTransition] = useTransition()

  async function onSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setServerError(null)

    try {
      await login({ phoneNumber, password })
      startTransition(() => {
        router.replace('/overview')
      })
    } catch (error) {
      setServerError(getApiErrorMessage(error, 'Не удалось открыть панель модератора.'))
    }
  }

  return (
    <Card className="admin-glass max-w-lg rounded-[2rem] border-white/60 bg-white/95 shadow-2xl shadow-slate-950/10">
      <CardHeader className="space-y-3">
        <StatusBadge tone="info">Только для модератора</StatusBadge>
        <CardTitle className="admin-display text-4xl text-slate-950">Вход в admin control</CardTitle>
        <CardDescription className="text-sm leading-6 text-slate-600">
          Управление пользователями, аптеками, каталогом и операционной картиной всей платформы.
        </CardDescription>
      </CardHeader>
      <CardContent>
        <form className="space-y-5" onSubmit={onSubmit}>
          <div className="space-y-2">
            <label className="text-sm font-medium text-slate-700" htmlFor="admin-phone">
              Телефон
            </label>
            <Input
              id="admin-phone"
              autoComplete="tel"
              value={phoneNumber}
              onChange={(event) => setPhoneNumber(event.target.value)}
              placeholder="+994509990003"
            />
          </div>

          <div className="space-y-2">
            <label className="text-sm font-medium text-slate-700" htmlFor="admin-password">
              Пароль
            </label>
            <Input
              id="admin-password"
              type="password"
              autoComplete="current-password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              placeholder="Введите пароль"
            />
          </div>

          {serverError ? (
            <div className="rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
              {serverError}
            </div>
          ) : null}

          <Button className="w-full rounded-full" type="submit" disabled={isPending}>
            {isPending ? 'Открываем панель...' : 'Войти'}
          </Button>
        </form>
      </CardContent>
    </Card>
  )
}
