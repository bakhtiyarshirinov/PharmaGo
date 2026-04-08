import Link from 'next/link'
import { userRoutes } from '@pharmago/config'
import { Button } from '@pharmago/ui'

export function AppShell({
  children,
  actions,
}: {
  children: React.ReactNode
  actions?: React.ReactNode
}) {
  return (
    <div className="min-h-screen">
      <header className="sticky top-0 z-40 border-b border-white/60 bg-stone-50/90 backdrop-blur">
        <div className="mx-auto flex max-w-7xl items-center justify-between px-4 py-4 md:px-6">
          <div className="flex items-center gap-8">
            <Link href={userRoutes.home} className="text-lg font-semibold text-slate-950">
              PharmaGo
            </Link>
            <nav className="hidden items-center gap-5 text-sm text-slate-600 md:flex">
              <Link href={userRoutes.medicines}>Medicines</Link>
              <Link href={userRoutes.pharmacies}>Pharmacies</Link>
              <Link href={userRoutes.reservations}>Reservations</Link>
              <Link href={userRoutes.notifications}>Notifications</Link>
            </nav>
          </div>
          <div className="flex items-center gap-3">
            {actions}
            <Button asChild variant="outline">
              <Link href={userRoutes.login}>Sign in</Link>
            </Button>
          </div>
        </div>
      </header>
      <main className="mx-auto max-w-7xl px-4 py-8 md:px-6">{children}</main>
    </div>
  )
}

