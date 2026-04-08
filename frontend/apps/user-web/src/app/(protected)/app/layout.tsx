import { readSessionMeta, requirePortalAccess } from '@pharmago/auth/server'
import { AppShell } from '../../../components/AppShell'

export default async function ProtectedLayout({ children }: { children: React.ReactNode }) {
  const session = await readSessionMeta()
  requirePortalAccess(session, 'user', '/auth/login')

  return <AppShell>{children}</AppShell>
}
