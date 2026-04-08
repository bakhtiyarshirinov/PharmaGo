import { readSessionMeta, requirePortalAccess } from '@pharmago/auth/server'
import { AppShell } from '../../../components/AppShell'
import { RealtimeBridge } from '../../../modules/realtime/RealtimeBridge'

export default async function ProtectedLayout({ children }: { children: React.ReactNode }) {
  const session = await readSessionMeta()
  requirePortalAccess(session, 'user', '/auth/login')

  return (
    <>
      <RealtimeBridge />
      <AppShell>{children}</AppShell>
    </>
  )
}
