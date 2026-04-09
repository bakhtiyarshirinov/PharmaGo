import { readSessionMeta } from '@pharmago/auth/server'
import { CockpitScreen } from '../../../modules/dashboard/CockpitScreen'

export default async function CockpitPage() {
  const session = await readSessionMeta('pharmacist')

  return <CockpitScreen initialSession={session} />
}
