'use client'

import Link from 'next/link'
import { useAuth } from '@pharmago/auth/client'
import { Button, Card, CardContent, CardHeader, CardTitle, EmptyState, PageHeader, StatusBadge } from '@pharmago/ui'
import { QueryStateCard } from '../../../../components/QueryStateCard'
import { useProfile } from '../../../../modules/profile/queries'
import { formatDateTime } from '../../../../lib/format'

export default function ProfilePage() {
  const auth = useAuth()
  const profile = useProfile()

  if (profile.isPending) {
    return (
      <div className="space-y-8">
        <PageHeader eyebrow="Profile" title="Your account and personalization" description="Manage your consumer identity and quick access rails." />
        <div className="grid gap-6 xl:grid-cols-[1.1fr,0.9fr]">
          <Card className="animate-pulse">
            <CardHeader>
              <div className="h-6 w-48 rounded-full bg-slate-200" />
            </CardHeader>
            <CardContent className="space-y-3">
              <div className="h-16 rounded-2xl bg-slate-100" />
              <div className="h-16 rounded-2xl bg-slate-100" />
            </CardContent>
          </Card>
          <Card className="animate-pulse">
            <CardHeader>
              <div className="h-6 w-40 rounded-full bg-slate-200" />
            </CardHeader>
            <CardContent className="space-y-3">
              <div className="h-14 rounded-2xl bg-slate-100" />
              <div className="h-14 rounded-2xl bg-slate-100" />
            </CardContent>
          </Card>
        </div>
      </div>
    )
  }

  if (profile.isError) {
    return (
      <div className="space-y-8">
        <PageHeader eyebrow="Profile" title="Your account and personalization" description="Manage your consumer identity and quick access rails." />
        <QueryStateCard
          title="Unable to load profile"
          description="The profile endpoint did not return successfully. Retry to fetch your current account snapshot."
          onAction={() => profile.refetch()}
        />
      </div>
    )
  }

  if (!profile.data) {
    return (
      <div className="space-y-8">
        <PageHeader eyebrow="Profile" title="Your account and personalization" description="Manage your consumer identity and quick access rails." />
        <EmptyState title="Profile unavailable" description="Sign in again to restore your account session." />
      </div>
    )
  }

  return (
    <div className="space-y-8">
      <PageHeader
        eyebrow="Profile"
        title={`${profile.data.firstName} ${profile.data.lastName}`}
        description="Consumer identity, session context and shortcuts into your personal medicine and pharmacy rails."
        actions={<StatusBadge tone="success">Consumer account</StatusBadge>}
      />

      <div className="grid gap-6 xl:grid-cols-[1.1fr,0.9fr]">
        <Card>
          <CardHeader>
            <CardTitle>Account overview</CardTitle>
          </CardHeader>
          <CardContent className="grid gap-4 md:grid-cols-2">
            <div className="rounded-2xl bg-slate-50 p-4">
              <p className="text-xs uppercase tracking-wide text-slate-400">Phone</p>
              <p className="mt-1 text-sm font-medium text-slate-950">{profile.data.phoneNumber}</p>
            </div>
            <div className="rounded-2xl bg-slate-50 p-4">
              <p className="text-xs uppercase tracking-wide text-slate-400">Email</p>
              <p className="mt-1 text-sm font-medium text-slate-950">{profile.data.email || 'Not provided'}</p>
            </div>
            <div className="rounded-2xl bg-slate-50 p-4">
              <p className="text-xs uppercase tracking-wide text-slate-400">Telegram</p>
              <p className="mt-1 text-sm font-medium text-slate-950">{profile.data.telegramUsername || 'Not linked'}</p>
            </div>
            <div className="rounded-2xl bg-slate-50 p-4">
              <p className="text-xs uppercase tracking-wide text-slate-400">Session expires</p>
              <p className="mt-1 text-sm font-medium text-slate-950">{formatDateTime(auth.session?.expiresAtUtc)}</p>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Personalization shortcuts</CardTitle>
          </CardHeader>
          <CardContent className="space-y-3">
            <Button asChild className="w-full justify-start">
              <Link href="/app/medicines/favorites">Favorite medicines</Link>
            </Button>
            <Button asChild variant="outline" className="w-full justify-start">
              <Link href="/app/medicines/recent">Recent medicines</Link>
            </Button>
            <Button asChild className="w-full justify-start">
              <Link href="/app/pharmacies/favorites">Favorite pharmacies</Link>
            </Button>
            <Button asChild variant="outline" className="w-full justify-start">
              <Link href="/app/pharmacies/recent">Recent pharmacies</Link>
            </Button>
            <Button
              variant="ghost"
              className="w-full justify-start"
              onClick={async () => {
                await auth.logout()
                window.location.href = '/auth/login'
              }}
            >
              Logout
            </Button>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
