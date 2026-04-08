import Link from 'next/link'
import { readSessionMeta, requirePortalAccess } from '@pharmago/auth/server'

export default async function ProtectedLayout({ children }: { children: React.ReactNode }) {
  const session = await readSessionMeta()
  requirePortalAccess(session, 'pharmacist', '/login')

  return (
    <div className="min-h-screen">
      <aside className="border-b border-slate-800 bg-slate-950 px-6 py-4">
        <div className="mx-auto flex max-w-7xl items-center justify-between">
          <div>
            <p className="text-sm uppercase tracking-[0.24em] text-emerald-400">Pharmacist workspace</p>
            <h1 className="text-xl font-semibold text-white">Operations cockpit</h1>
          </div>
          <nav className="flex items-center gap-4 text-sm text-slate-300">
            <Link href="/cockpit">Cockpit</Link>
            <Link href="/reservations">Reservations</Link>
            <Link href="/inventory">Inventory</Link>
            <Link href="/notifications">Notifications</Link>
          </nav>
        </div>
      </aside>
      <main className="mx-auto max-w-7xl px-6 py-8">{children}</main>
    </div>
  )
}
