import { readSessionMeta } from '@pharmago/auth/server'
import { InventoryScreen } from '../../../modules/inventory/InventoryScreen'

export default async function InventoryPage() {
  const session = await readSessionMeta()

  return <InventoryScreen initialSession={session} />
}
