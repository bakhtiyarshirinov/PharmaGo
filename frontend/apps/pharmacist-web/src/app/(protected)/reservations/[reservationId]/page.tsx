import { readSessionMeta } from '@pharmago/auth/server'
import { ReservationDetailScreen } from '../../../../modules/reservations/ReservationDetailScreen'

export default async function ReservationDetailPage({
  params,
}: {
  params: Promise<{ reservationId: string }>
}) {
  const { reservationId } = await params
  const session = await readSessionMeta('pharmacist')

  return <ReservationDetailScreen reservationId={reservationId} initialSession={session} />
}
