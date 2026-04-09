import { readSessionMeta } from '@pharmago/auth/server'
import { AuditLogsScreen } from '../../../modules/audit/AuditLogsScreen'

export default async function AuditLogsPage() {
  const session = await readSessionMeta('admin')

  return <AuditLogsScreen initialSession={session} />
}
