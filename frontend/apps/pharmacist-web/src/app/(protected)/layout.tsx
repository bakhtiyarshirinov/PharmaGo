import Link from 'next/link'
import { readSessionMeta, requirePortalAccess } from '@pharmago/auth/server'
import { StatusBadge } from '@pharmago/ui'

export default async function ProtectedLayout({ children }: { children: React.ReactNode }) {
  const session = await readSessionMeta()
  requirePortalAccess(session, 'pharmacist', '/login')

  const staffName = session ? `${session.user.firstName} ${session.user.lastName}` : 'PharmaGo Staff'

  return (
    <div className="min-h-screen bg-slate-950">
      <aside className="border-b border-white/10 bg-slate-950/90 px-6 py-4 backdrop-blur">
        <div className="mx-auto flex max-w-7xl flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
          <div className="space-y-2">
            <p className="text-xs font-semibold uppercase tracking-[0.28em] text-emerald-400">PharmaGo Pharmacist</p>
            <div className="flex flex-wrap items-center gap-3">
              <h1 className="text-2xl font-semibold text-white">Операционный контур аптеки</h1>
              <StatusBadge tone="info">{staffName}</StatusBadge>
            </div>
          </div>
          <nav className="flex flex-wrap items-center gap-2 text-sm text-slate-300">
            <NavLink href="/cockpit">Кокпит</NavLink>
            <NavLink href="/reservations">Резервы</NavLink>
            <NavLink href="/inventory">Склад</NavLink>
            <NavLink href="/notifications">Уведомления</NavLink>
          </nav>
        </div>
      </aside>
      <main className="mx-auto max-w-7xl px-6 py-8">{children}</main>
    </div>
  )
}

function NavLink({ href, children }: { href: string; children: React.ReactNode }) {
  return (
    <Link
      href={href}
      className="rounded-full border border-white/10 px-4 py-2 text-slate-200 transition hover:border-emerald-500/60 hover:bg-white/5"
    >
      {children}
    </Link>
  )
}
