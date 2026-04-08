'use client'

import { zodResolver } from '@hookform/resolvers/zod'
import { useAuth } from '@pharmago/auth/client'
import { Button, Card, CardContent, CardDescription, CardHeader, CardTitle, Input, StatusBadge } from '@pharmago/ui'
import { useRouter, useSearchParams } from 'next/navigation'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { getApiErrorMessage } from '../../../lib/errors'

const schema = z.object({
  phoneNumber: z.string().min(7),
  password: z.string().min(8),
})

type LoginValues = z.infer<typeof schema>

export function LoginForm() {
  const auth = useAuth()
  const router = useRouter()
  const searchParams = useSearchParams()
  const [serverError, setServerError] = useState<string | null>(null)
  const redirectTo = searchParams.get('redirect') ?? '/app/reservations'
  const form = useForm<LoginValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      phoneNumber: '',
      password: '',
    },
  })

  return (
    <Card className="consumer-glass mx-auto max-w-md rounded-[2rem] border-0">
      <CardHeader className="space-y-3">
        <StatusBadge tone="info">Consumer portal</StatusBadge>
        <CardTitle className="consumer-display text-3xl text-slate-950">Sign in to PharmaGo</CardTitle>
        <CardDescription className="text-sm leading-6 text-slate-600">
          Pick up your reservation flow where you left it, from medicine discovery to the final pickup update.
        </CardDescription>
      </CardHeader>
      <CardContent>
        <form
          className="space-y-4"
          onSubmit={form.handleSubmit(async (values) => {
            setServerError(null)

            try {
              await auth.login(values)
              router.push(redirectTo)
            } catch (error) {
              setServerError(getApiErrorMessage(error, 'Unable to sign in right now.'))
            }
          })}
        >
          <div className="space-y-2">
            <label className="text-sm font-medium text-slate-700">Phone number</label>
            <Input {...form.register('phoneNumber')} placeholder="+994..." />
          </div>
          <div className="space-y-2">
            <label className="text-sm font-medium text-slate-700">Password</label>
            <Input type="password" {...form.register('password')} placeholder="Password" />
          </div>
          {serverError ? (
            <div className="rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
              {serverError}
            </div>
          ) : null}
          <Button className="w-full rounded-full" type="submit">
            Continue
          </Button>
        </form>
      </CardContent>
    </Card>
  )
}
