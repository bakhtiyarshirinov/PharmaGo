'use client'

import { zodResolver } from '@hookform/resolvers/zod'
import { useAuth } from '@pharmago/auth/client'
import { Button, Card, CardContent, CardHeader, CardTitle, Input } from '@pharmago/ui'
import { useRouter, useSearchParams } from 'next/navigation'
import { useForm } from 'react-hook-form'
import { z } from 'zod'

const schema = z.object({
  phoneNumber: z.string().min(7),
  password: z.string().min(8),
})

type LoginValues = z.infer<typeof schema>

export function LoginForm() {
  const auth = useAuth()
  const router = useRouter()
  const searchParams = useSearchParams()
  const redirectTo = searchParams.get('redirect') ?? '/app/reservations'
  const form = useForm<LoginValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      phoneNumber: '',
      password: '',
    },
  })

  return (
    <Card className="mx-auto max-w-md">
      <CardHeader>
        <CardTitle>Sign in to PharmaGo</CardTitle>
      </CardHeader>
      <CardContent>
        <form
          className="space-y-4"
          onSubmit={form.handleSubmit(async (values) => {
            await auth.login(values)
            router.push(redirectTo)
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
          <Button className="w-full" type="submit">
            Continue
          </Button>
        </form>
      </CardContent>
    </Card>
  )
}
