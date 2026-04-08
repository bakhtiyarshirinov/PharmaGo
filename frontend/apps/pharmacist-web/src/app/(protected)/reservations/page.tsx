import { readSessionMeta } from '@pharmago/auth/server'
import { ReservationQueueScreen } from '../../../modules/reservations/ReservationQueueScreen'

export default async function PharmacistReservationsPage() {
  const session = await readSessionMeta()

  return <ReservationQueueScreen initialSession={session} />
}
