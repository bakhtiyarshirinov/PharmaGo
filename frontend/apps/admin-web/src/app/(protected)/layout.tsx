import Link from 'next/link'
import { readSessionMeta, requirePortalAccess } from '@pharmago/auth/server'

export default async function ProtectedLayout({ children }: { children: React.ReactNode }) {
  const session = await readSessionMeta()
  requirePortalAccess(session, 'admin', '/login')

  return (
    <div className="min-h-screen">
      <aside className="border-b border-slate-200 bg-white px-6 py-4">
        <div className="mx-auto flex max-w-7xl items-center justify-between">
          <div>
            <p className="text-sm uppercase tracking-[0.24em] text-sky-700">Admin control center</p>
            <h1 className="text-xl font-semibold text-slate-950">Platform administration</h1>
          </div>
          <nav className="flex items-center gap-4 text-sm text-slate-600">
            <Link href="/overview">Overview</Link>
            <Link href="/users">Users</Link>
            <Link href="/pharmacies">Pharmacies</Link>
            <Link href="/master-data/medicines">Medicines</Link>
            <Link href="/audit-logs">Audit</Link>
          </nav>
        </div>
      </aside>
      <main className="mx-auto max-w-7xl px-6 py-8">{children}</main>
    </div>
  )
}
