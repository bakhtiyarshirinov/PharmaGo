'use client'

import Link from 'next/link'
import { usePathname } from 'next/navigation'
import { userRoutes } from '@pharmago/config'
import { Button } from '@pharmago/ui'

export function AppShell({
  children,
  actions,
}: {
  children: React.ReactNode
  actions?: React.ReactNode
}) {
  const pathname = usePathname()

  const navItems = [
    { href: userRoutes.medicines, label: 'Medicines' },
    { href: userRoutes.pharmacies, label: 'Pharmacies' },
    { href: userRoutes.reservations, label: 'Reservations' },
    { href: userRoutes.notifications, label: 'Notifications' },
    { href: userRoutes.profile, label: 'Profile' },
  ]

  return (
    <div className="consumer-shell min-h-screen">
      <header className="sticky top-0 z-40 px-4 pt-4 md:px-6">
        <div className="consumer-glass mx-auto flex max-w-7xl flex-wrap items-center justify-between gap-4 rounded-[2rem] px-5 py-4 md:px-6">
          <div className="flex items-center gap-6">
            <Link href={userRoutes.home} className="flex items-center gap-3">
              <span className="inline-flex h-11 w-11 items-center justify-center rounded-2xl bg-gradient-to-br from-orange-500 via-amber-400 to-emerald-500 text-sm font-extrabold text-white shadow-lg shadow-orange-900/20">
                PG
              </span>
              <div>
                <p className="consumer-display text-xl font-semibold text-slate-950">PharmaGo</p>
                <p className="text-xs uppercase tracking-[0.26em] text-slate-500">Reserve with clarity</p>
              </div>
            </Link>
            <nav className="hidden items-center gap-2 md:flex">
              {navItems.map((item) => {
                const isActive = pathname === item.href || pathname.startsWith(`${item.href}/`)

                return (
                  <Link
                    key={item.href}
                    href={item.href}
                    className={[
                      'rounded-full px-4 py-2 text-sm font-medium transition',
                      isActive
                        ? 'bg-white text-slate-950 shadow-[0_12px_32px_-20px_rgba(15,23,42,0.45)]'
                        : 'text-slate-600 hover:bg-white/70 hover:text-slate-950',
                    ].join(' ')}
                  >
                    {item.label}
                  </Link>
                )
              })}
            </nav>
          </div>
          <div className="flex items-center gap-3">
            {actions}
            <Button asChild className="rounded-full" variant="outline">
              <Link href={userRoutes.login}>Sign in</Link>
            </Button>
          </div>
        </div>
      </header>
      <main className="mx-auto max-w-7xl px-4 py-8 md:px-6">
        <div className="soft-enter">{children}</div>
      </main>
    </div>
  )
}
