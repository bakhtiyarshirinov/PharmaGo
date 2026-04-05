import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Grid, Stack, TextField, Button } from '@mui/material'
import { useLocation } from 'react-router-dom'
import { useState } from 'react'
import { actOnReservation, createReservation, getActiveReservations, getMyReservations, getReservationTimeline } from '../../shared/api/reservations'
import { SectionCard } from '../../shared/ui/SectionCard'
import { useSessionStore } from '../../features/auth/model/useSessionStore'
import { EmptyState } from '../../shared/ui/EmptyState'
import LoginRoundedIcon from '@mui/icons-material/LoginRounded'
import { useAppUiStore } from '../../features/app/model/useAppUiStore'
import { EntityList, SmallAction } from '../../shared/ui/EntityList'
import { reservationStatusLabels } from '../../shared/config/roles'
import { formatDate, normalizeItems } from '../../shared/lib/format'

export function ReservationsPage() {
  const location = useLocation()
  const session = useSessionStore((state) => state.session)
  const setError = useAppUiStore((state) => state.setError)
  const setToast = useAppUiStore((state) => state.setToast)
  const queryClient = useQueryClient()
  const [draft, setDraft] = useState(() => ({
    pharmacyId: location.state?.pharmacyId || '',
    medicineId: location.state?.medicineId || '',
    quantity: 1,
    reserveForHours: 2,
  }))
  const [timelineReservationId, setTimelineReservationId] = useState('')

  const myReservationsQuery = useQuery({
    queryKey: ['reservations', 'my'],
    queryFn: getMyReservations,
    enabled: Boolean(session),
  })

  const activeReservationsQuery = useQuery({
    queryKey: ['reservations', 'active'],
    queryFn: getActiveReservations,
    enabled: Boolean(session),
  })

  const timelineQuery = useQuery({
    queryKey: ['reservations', 'timeline', timelineReservationId],
    queryFn: () => getReservationTimeline(timelineReservationId),
    enabled: Boolean(session && timelineReservationId),
  })

  const createMutation = useMutation({
    mutationFn: createReservation,
    onSuccess: () => {
      setToast('Reservation created')
      queryClient.invalidateQueries({ queryKey: ['reservations', 'my'] })
      queryClient.invalidateQueries({ queryKey: ['reservations', 'active'] })
      queryClient.invalidateQueries({ queryKey: ['medicines', 'popular'] })
      queryClient.invalidateQueries({ queryKey: ['pharmacies', 'popular'] })
    },
    onError: (error) => setError(error.message),
  })

  const actionMutation = useMutation({
    mutationFn: ({ id, action }) => actOnReservation(id, action),
    onSuccess: (_, variables) => {
      setToast(`Reservation ${variables.action.replace(/-/g, ' ')} executed`)
      queryClient.invalidateQueries({ queryKey: ['reservations', 'my'] })
      queryClient.invalidateQueries({ queryKey: ['reservations', 'active'] })
      queryClient.invalidateQueries({ queryKey: ['reservations', 'timeline', variables.id] })
    },
    onError: (error) => setError(error.message),
  })

  const myReservations = normalizeItems(myReservationsQuery.data)
  const activeReservations = normalizeItems(activeReservationsQuery.data)
  const timeline = normalizeItems(timelineQuery.data)

  return (
    <Grid container spacing={3}>
      <Grid size={{ xs: 12, lg: 4 }}>
        <SectionCard title="Create reservation" subtitle="Plug straight into the backend reservation workflow">
          {session ? (
            <Stack spacing={2}>
              <TextField
                label="Pharmacy ID"
                value={draft.pharmacyId}
                onChange={(event) => setDraft((current) => ({ ...current, pharmacyId: event.target.value }))}
              />
              <TextField
                label="Medicine ID"
                value={draft.medicineId}
                onChange={(event) => setDraft((current) => ({ ...current, medicineId: event.target.value }))}
              />
              <TextField
                label="Quantity"
                type="number"
                value={draft.quantity}
                onChange={(event) => setDraft((current) => ({ ...current, quantity: Number(event.target.value) }))}
              />
              <TextField
                label="Reserve for hours"
                type="number"
                value={draft.reserveForHours}
                onChange={(event) => setDraft((current) => ({ ...current, reserveForHours: Number(event.target.value) }))}
              />
              <Button
                variant="contained"
                size="large"
                onClick={() =>
                  createMutation.mutate({
                    pharmacyId: draft.pharmacyId,
                    reserveForHours: Number(draft.reserveForHours),
                    items: [{ medicineId: draft.medicineId, quantity: Number(draft.quantity) }],
                  })
                }
              >
                Create reservation
              </Button>
            </Stack>
          ) : (
            <EmptyState
              icon={<LoginRoundedIcon fontSize="large" />}
              title="Sign in to reserve"
              description="Reservation creation is wired up, but it needs an authenticated user session."
            />
          )}
        </SectionCard>
      </Grid>

      <Grid size={{ xs: 12, lg: 8 }}>
        <Stack spacing={3}>
          <SectionCard title="Active reservations" subtitle="Operational visibility for what still matters right now">
            <ReservationEntityList items={activeReservations} onTimeline={setTimelineReservationId} onAction={actionMutation.mutate} />
          </SectionCard>

          <Grid container spacing={3}>
            <Grid size={{ xs: 12, xl: 7 }}>
              <SectionCard title="My reservations" subtitle="Personal reservation history and action surface">
                <ReservationEntityList items={myReservations} onTimeline={setTimelineReservationId} onAction={actionMutation.mutate} />
              </SectionCard>
            </Grid>
            <Grid size={{ xs: 12, xl: 5 }}>
              <SectionCard
                title="Timeline"
                subtitle={timelineReservationId ? `Reservation ${timelineReservationId}` : 'Open any reservation timeline'}
              >
                <EntityList
                  items={timeline}
                  primaryKey="occurredAtUtc"
                  titleKey="title"
                  subtitleRenderer={(item) => `${item.description || item.action || 'Reservation event'} • ${formatDate(item.occurredAtUtc)}`}
                  emptyLabel="Timeline events will appear here after you open a reservation."
                />
              </SectionCard>
            </Grid>
          </Grid>
        </Stack>
      </Grid>
    </Grid>
  )
}

function ReservationEntityList({ items, onTimeline, onAction }) {
  return (
    <EntityList
      items={items}
      primaryKey="reservationId"
      titleKey="reservationNumber"
      subtitleRenderer={(item) =>
        `${item.pharmacyName || 'Pharmacy'} • ${reservationStatusLabels[item.status] || 'Status'} • until ${formatDate(item.reservedUntilUtc)}`
      }
      trailingRenderer={(item) => (
        <Stack direction="row" spacing={1}>
          <SmallAction onClick={() => onTimeline(item.reservationId)}>Timeline</SmallAction>
          {item.status === 1 || item.status === 2 || item.status === 3 ? (
            <SmallAction color="secondary" onClick={() => onAction({ id: item.reservationId, action: 'cancel' })}>
              Cancel
            </SmallAction>
          ) : null}
        </Stack>
      )}
      emptyLabel="No reservations found yet."
    />
  )
}
