import Link from 'next/link'
import { readSessionMeta, requirePortalAccess } from '@pharmago/auth/server'
import { StatusBadge } from '@pharmago/ui'
import { RealtimeBridge } from '../../modules/realtime/RealtimeBridge'

export default async function ProtectedLayout({ children }: { children: React.ReactNode }) {
  const session = await readSessionMeta('pharmacist')
  requirePortalAccess(session, 'pharmacist', '/login')

  const staffName = session ? `${session.user.firstName} ${session.user.lastName}` : 'PharmaGo Staff'

  return (
    <div className="min-h-screen bg-slate-950">
      <RealtimeBridge />
      <aside className="px-6 pt-6">
        <div className="ops-glass mx-auto flex max-w-7xl flex-col gap-5 rounded-[2rem] px-6 py-5 lg:flex-row lg:items-center lg:justify-between">
          <div className="space-y-2">
            <p className="text-xs font-semibold uppercase tracking-[0.28em] text-emerald-300">PharmaGo Pharmacist</p>
            <div className="flex flex-wrap items-center gap-3">
              <h1 className="ops-display text-3xl font-semibold text-white">Операционный контур аптеки</h1>
              <StatusBadge tone="info">{staffName}</StatusBadge>
            </div>
            <p className="max-w-2xl text-sm text-slate-300">
              Быстрый staff-контур для очереди резервов, складского давления и реакций на события смены без лишних переходов.
            </p>
          </div>
          <nav className="flex flex-wrap items-center gap-2 text-sm text-slate-300">
            <NavLink href="/cockpit">Кокпит</NavLink>
            <NavLink href="/reservations">Резервы</NavLink>
            <NavLink href="/inventory">Склад</NavLink>
            <NavLink href="/notifications">Уведомления</NavLink>
          </nav>
        </div>
      </aside>
      <main className="mx-auto max-w-7xl px-6 py-8">
        <div className="ops-enter">{children}</div>
      </main>
    </div>
  )
}

function NavLink({ href, children }: { href: string; children: React.ReactNode }) {
  return (
    <Link
      href={href}
      className="rounded-full border border-white/10 bg-white/[0.03] px-4 py-2 text-slate-200 transition hover:border-emerald-400/60 hover:bg-white/[0.08] hover:text-white"
    >
      {children}
    </Link>
  )
}
