import { readSessionMeta } from '@pharmago/auth/server'
import { NotificationsScreen } from '../../../modules/notifications/NotificationsScreen'

export default async function NotificationsPage() {
  const session = await readSessionMeta()

  return <NotificationsScreen initialSession={session} />
}
