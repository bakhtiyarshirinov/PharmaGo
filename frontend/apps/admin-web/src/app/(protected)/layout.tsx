import Link from 'next/link'
import { readSessionMeta, requirePortalAccess } from '@pharmago/auth/server'
import { StatusBadge } from '@pharmago/ui'

export default async function ProtectedLayout({ children }: { children: React.ReactNode }) {
  const session = await readSessionMeta()
  requirePortalAccess(session, 'admin', '/login')
  const staffName = session ? `${session.user.firstName} ${session.user.lastName}` : 'Moderator'

  return (
    <div className="min-h-screen">
      <aside className="border-b border-slate-200/70 bg-white/80 px-6 py-4 backdrop-blur">
        <div className="admin-glass mx-auto flex max-w-7xl flex-col gap-4 rounded-[2rem] px-6 py-5 lg:flex-row lg:items-center lg:justify-between">
          <div className="space-y-2">
            <p className="text-xs font-semibold uppercase tracking-[0.28em] text-teal-700">Admin control center</p>
            <div className="flex flex-wrap items-center gap-3">
              <h1 className="admin-display text-3xl text-slate-950">Platform administration</h1>
              <StatusBadge tone="info">{staffName}</StatusBadge>
            </div>
          </div>
          <nav className="flex flex-wrap items-center gap-2 text-sm text-slate-700">
            <NavLink href="/overview">Overview</NavLink>
            <NavLink href="/users">Users</NavLink>
            <NavLink href="/pharmacies">Pharmacies</NavLink>
            <NavLink href="/master-data/medicines">Medicines</NavLink>
            <NavLink href="/audit-logs">Audit</NavLink>
          </nav>
        </div>
      </aside>
      <main className="admin-enter mx-auto max-w-7xl px-6 py-8">{children}</main>
    </div>
  )
}

function NavLink({ href, children }: { href: string; children: React.ReactNode }) {
  return (
    <Link
      href={href}
      className="rounded-full border border-slate-200/80 bg-white/70 px-4 py-2 transition hover:border-teal-500/50 hover:bg-white"
    >
      {children}
    </Link>
  )
}
