import { readSessionMeta } from '@pharmago/auth/server'
import { OverviewScreen } from '../../../modules/overview/OverviewScreen'

export default async function AdminOverviewPage() {
  const session = await readSessionMeta('admin')

  return <OverviewScreen initialSession={session} />
}
