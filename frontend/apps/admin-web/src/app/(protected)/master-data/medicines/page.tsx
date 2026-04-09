import { readSessionMeta } from '@pharmago/auth/server'
import { MedicinesScreen } from '../../../../modules/master-data/MedicinesScreen'

export default async function AdminMedicinesPage() {
  const session = await readSessionMeta()

  return <MedicinesScreen initialSession={session} />
}
