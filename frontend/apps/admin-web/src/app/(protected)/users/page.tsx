import { readSessionMeta } from '@pharmago/auth/server'
import { UsersScreen } from '../../../modules/users/UsersScreen'

export default async function UsersPage() {
  const session = await readSessionMeta()

  return <UsersScreen initialSession={session} />
}
