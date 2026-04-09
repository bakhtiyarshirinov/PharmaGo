import { readSessionMeta } from '@pharmago/auth/server'
import { PharmaciesScreen } from '../../../modules/pharmacies/PharmaciesScreen'

export default async function AdminPharmaciesPage() {
  const session = await readSessionMeta()

  return <PharmaciesScreen initialSession={session} />
}
