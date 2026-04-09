import { readSessionMeta } from '@pharmago/auth/server'
import { InventoryScreen } from '../../../modules/inventory/InventoryScreen'

export default async function InventoryPage() {
  const session = await readSessionMeta('pharmacist')

  return <InventoryScreen initialSession={session} />
}
