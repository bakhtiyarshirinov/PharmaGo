import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Button, Grid, Stack, TextField, Typography } from '@mui/material'
import AccessTimeRoundedIcon from '@mui/icons-material/AccessTimeRounded'
import Inventory2RoundedIcon from '@mui/icons-material/Inventory2Rounded'
import EventAvailableRoundedIcon from '@mui/icons-material/EventAvailableRounded'
import { useLocation } from 'react-router-dom'
import { useMemo, useState } from 'react'
import { actOnReservation, createReservation, getActiveReservations, getMyReservations, getReservationTimeline } from '../../shared/api/reservations'
import { SectionCard } from '../../shared/ui/SectionCard'
import { useSessionStore } from '../../features/auth/model/useSessionStore'
import { EmptyState } from '../../shared/ui/EmptyState'
import LoginRoundedIcon from '@mui/icons-material/LoginRounded'
import { useAppUiStore } from '../../features/app/model/useAppUiStore'
import { EntityList, MetaChip, SmallAction } from '../../shared/ui/EntityList'
import { reservationStatusLabels } from '../../shared/config/roles'
import { formatDate, formatMoney, normalizeItems } from '../../shared/lib/format'
import { HeroAction, HeroSurface } from '../../shared/ui/HeroSurface'
import { InfoCard } from '../../shared/ui/InfoCard'

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
  const nextReservation = activeReservations[0] || myReservations[0]
  const totalValue = useMemo(
    () => myReservations.reduce((sum, item) => sum + Number(item.totalAmount || 0), 0),
    [myReservations],
  )
  const draftReady = Boolean(draft.pharmacyId && draft.medicineId && Number(draft.quantity) > 0 && Number(draft.reserveForHours) > 0)
  const highlightedReservation = nextReservation || myReservations.find((item) => item.status !== 4 && item.status !== 5 && item.status !== 6)
  const timelineSubtitle = timelineReservationId
    ? `Reservation ${timelineReservationId}`
    : highlightedReservation
      ? `Suggested: open ${highlightedReservation.reservationNumber}`
      : 'Open any reservation timeline'

  return (
    <Stack spacing={3}>
      <HeroSurface
        eyebrow={<MetaChip label="Reservation journey" color="secondary" />}
        title="Бронирование теперь выглядит как пользовательский поток, а не как служебная форма."
        description="Здесь собраны создание брони, активные заказы и timeline. Следующий логичный шаг после этого — уже notifications и напоминания."
        badges={[
          <MetaChip key="r1" label={`${activeReservations.length} active`} color="success" />,
          <MetaChip key="r2" label={`${myReservations.length} total reservations`} color="primary" />,
          <MetaChip key="r3" label={timelineReservationId ? 'Timeline open' : 'Timeline ready'} />,
        ]}
        actions={[
          <HeroAction
            key="ra1"
            startIcon={<Inventory2RoundedIcon />}
            onClick={() => setTimelineReservationId(highlightedReservation?.reservationId || '')}
            disabled={!highlightedReservation}
          >
            Open next reservation
          </HeroAction>,
          <Button
            key="ra2"
            variant="outlined"
            size="large"
            startIcon={<AccessTimeRoundedIcon />}
            onClick={() => setDraft((current) => ({ ...current, reserveForHours: 4 }))}
          >
            Extend reserve window
          </Button>,
        ]}
        metrics={[
          <Grid key="rm1" size={{ xs: 6 }}>
            <InfoCard title="Next reservation" value={nextReservation?.reservationNumber || 'None'} />
          </Grid>,
          <Grid key="rm2" size={{ xs: 6 }}>
            <InfoCard title="Reserve window" value={`${draft.reserveForHours} hours`} />
          </Grid>,
          <Grid key="rm3" size={{ xs: 6 }}>
            <InfoCard title="Active value" value={formatMoney(activeReservations.reduce((sum, item) => sum + Number(item.totalAmount || 0), 0))} />
          </Grid>,
          <Grid key="rm4" size={{ xs: 6 }}>
            <InfoCard title="Total booked" value={formatMoney(totalValue)} />
          </Grid>,
        ]}
      />

      <Grid container spacing={3}>
        <Grid size={{ xs: 12, lg: 4 }}>
          <SectionCard
            title="Create reservation"
            subtitle="Plug straight into the backend reservation workflow"
            accent="secondary"
            tone="warm"
            actions={<MetaChip label={draftReady ? 'Ready to submit' : 'Compose + submit'} color={draftReady ? 'success' : 'secondary'} />}
          >
            {session ? (
              <Stack spacing={2.25}>
                <Stack direction="row" spacing={1} flexWrap="wrap">
                  <MetaChip label="Reservation ready" color="primary" />
                  <MetaChip label={draft.medicineId ? 'Medicine attached' : 'Pick medicine'} color={draft.medicineId ? 'success' : 'default'} />
                  <MetaChip label={draft.pharmacyId ? 'Pharmacy attached' : 'Pick pharmacy'} color={draft.pharmacyId ? 'success' : 'default'} />
                  <MetaChip label={draftReady ? 'All required fields set' : 'Waiting for selection'} color={draftReady ? 'success' : 'default'} />
                </Stack>

                <Grid container spacing={2}>
                  <Grid size={{ xs: 12, sm: 6 }}>
                    <TextField
                      label="Pharmacy ID"
                      value={draft.pharmacyId}
                      onChange={(event) => setDraft((current) => ({ ...current, pharmacyId: event.target.value }))}
                      fullWidth
                    />
                  </Grid>
                  <Grid size={{ xs: 12, sm: 6 }}>
                    <TextField
                      label="Medicine ID"
                      value={draft.medicineId}
                      onChange={(event) => setDraft((current) => ({ ...current, medicineId: event.target.value }))}
                      fullWidth
                    />
                  </Grid>
                  <Grid size={{ xs: 12, sm: 6 }}>
                    <TextField
                      label="Quantity"
                      type="number"
                      value={draft.quantity}
                      onChange={(event) => setDraft((current) => ({ ...current, quantity: Number(event.target.value) }))}
                      fullWidth
                    />
                  </Grid>
                  <Grid size={{ xs: 12, sm: 6 }}>
                    <TextField
                      label="Reserve for hours"
                      type="number"
                      value={draft.reserveForHours}
                      onChange={(event) => setDraft((current) => ({ ...current, reserveForHours: Number(event.target.value) }))}
                      fullWidth
                    />
                  </Grid>
                </Grid>

                <Grid container spacing={2}>
                  <Grid size={{ xs: 12, md: 4 }}>
                    <InfoCard title="Pharmacy" value={draft.pharmacyId || 'Waiting'} />
                  </Grid>
                  <Grid size={{ xs: 12, md: 4 }}>
                    <InfoCard title="Medicine" value={draft.medicineId || 'Waiting'} />
                  </Grid>
                  <Grid size={{ xs: 12, md: 4 }}>
                    <InfoCard title="Quantity" value={draft.quantity || 0} />
                  </Grid>
                </Grid>

                <Button
                  variant="contained"
                  size="large"
                  startIcon={<Inventory2RoundedIcon />}
                  disabled={!draftReady || createMutation.isPending}
                  onClick={() =>
                    createMutation.mutate({
                      pharmacyId: draft.pharmacyId,
                      reserveForHours: Number(draft.reserveForHours),
                      items: [{ medicineId: draft.medicineId, quantity: Number(draft.quantity) }],
                    })
                  }
                >
                  {createMutation.isPending ? 'Creating reservation...' : 'Create reservation'}
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
            <Grid container spacing={2}>
              <Grid size={{ xs: 12, md: 4 }}>
                <SectionCard
                  title="Next pickup"
                  subtitle="The reservation most likely to matter next."
                  tone="cool"
                  actions={<MetaChip label={highlightedReservation ? 'Actionable' : 'Quiet'} color={highlightedReservation ? 'success' : 'default'} />}
                >
                  <Stack spacing={1.25}>
                    <Typography variant="h6">{highlightedReservation?.reservationNumber || 'No active reservation'}</Typography>
                    <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                      {highlightedReservation
                        ? `${highlightedReservation.pharmacyName || 'Pharmacy'} • ${reservationStatusLabels[highlightedReservation.status] || 'Status'}`
                        : 'Create or open a reservation to populate this slot.'}
                    </Typography>
                    <Button
                      variant="outlined"
                      size="small"
                      startIcon={<EventAvailableRoundedIcon />}
                      disabled={!highlightedReservation}
                      onClick={() => setTimelineReservationId(highlightedReservation?.reservationId || '')}
                    >
                      Open timeline
                    </Button>
                  </Stack>
                </SectionCard>
              </Grid>
              <Grid size={{ xs: 12, md: 4 }}>
                <SectionCard
                  title="Active items"
                  subtitle="Live reservations still moving through the workflow."
                  tone="warm"
                >
                  <Stack direction="row" spacing={1} flexWrap="wrap">
                    <MetaChip label={`${activeReservations.filter((item) => item.status === 1).length} pending`} color="primary" />
                    <MetaChip label={`${activeReservations.filter((item) => item.status === 2).length} confirmed`} color="secondary" />
                    <MetaChip label={`${activeReservations.filter((item) => item.status === 3).length} ready`} color="success" />
                  </Stack>
                </SectionCard>
              </Grid>
              <Grid size={{ xs: 12, md: 4 }}>
                <SectionCard
                  title="Customer posture"
                  subtitle="A quick read on how far the current flow has progressed."
                  tone="cool"
                >
                  <Stack direction="row" spacing={1} flexWrap="wrap">
                    <MetaChip label={session ? 'Signed in' : 'Guest'} color={session ? 'success' : 'default'} />
                    <MetaChip label={timelineReservationId ? 'Timeline open' : 'Timeline idle'} color={timelineReservationId ? 'primary' : 'default'} />
                    <MetaChip label={draftReady ? 'Draft ready' : 'Draft incomplete'} color={draftReady ? 'secondary' : 'default'} />
                  </Stack>
                </SectionCard>
              </Grid>
            </Grid>

            <SectionCard
              title="Active reservations"
              subtitle="Operational visibility for what still matters right now"
              tone="cool"
              actions={<MetaChip label={`${activeReservations.length} active now`} color="success" />}
            >
              <ReservationEntityList items={activeReservations} onTimeline={setTimelineReservationId} onAction={actionMutation.mutate} isBusy={actionMutation.isPending} />
            </SectionCard>

            <Grid container spacing={3}>
              <Grid size={{ xs: 12, xl: 7 }}>
                <SectionCard
                  title="My reservations"
                  subtitle="Personal reservation history and action surface"
                  actions={<MetaChip label={`${myReservations.length} total`} color="primary" />}
                >
                  <ReservationEntityList items={myReservations} onTimeline={setTimelineReservationId} onAction={actionMutation.mutate} isBusy={actionMutation.isPending} />
                </SectionCard>
              </Grid>
              <Grid size={{ xs: 12, xl: 5 }}>
                <SectionCard
                  title="Timeline"
                  subtitle={timelineSubtitle}
                  tone="cool"
                  actions={<MetaChip label={timelineReservationId ? 'Live timeline' : 'Awaiting selection'} color={timelineReservationId ? 'primary' : 'default'} />}
                >
                  <EntityList
                    items={timeline}
                    primaryKey="occurredAtUtc"
                    titleKey="title"
                    subtitleRenderer={(item) => `${item.description || item.action || 'Reservation event'} • ${formatDate(item.occurredAtUtc)}`}
                    metaRenderer={(item) => (
                      <>
                        <MetaChip label={item.status || item.action || 'Event'} color="primary" />
                        <MetaChip label={item.isSystemEvent ? 'System' : 'User'} />
                      </>
                    )}
                    emptyLabel="Timeline events will appear here after you open a reservation."
                  />
                </SectionCard>
              </Grid>
            </Grid>
          </Stack>
        </Grid>
      </Grid>
    </Stack>
  )
}

function ReservationEntityList({ items, onTimeline, onAction, isBusy }) {
  return (
    <EntityList
      items={items}
      primaryKey="reservationId"
      titleKey="reservationNumber"
      subtitleRenderer={(item) =>
        `${item.pharmacyName || 'Pharmacy'} • ${reservationStatusLabels[item.status] || 'Status'} • until ${formatDate(item.reservedUntilUtc)}`
      }
      metaRenderer={(item) => (
        <>
          <MetaChip label={reservationStatusLabels[item.status] || 'Status'} color={item.status === 4 ? 'success' : item.status === 5 || item.status === 6 ? 'default' : 'primary'} />
          <MetaChip label={item.totalAmount ? `$${Number(item.totalAmount).toFixed(2)}` : 'Amount pending'} color="secondary" />
          <MetaChip label={item.readyForPickupAtUtc ? 'Pickup ready' : item.confirmedAtUtc ? 'Confirmed' : 'In progress'} />
        </>
      )}
      trailingRenderer={(item) => (
        <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
          <SmallAction onClick={() => onTimeline(item.reservationId)}>Timeline</SmallAction>
          {item.status === 1 ? (
            <SmallAction color="secondary" onClick={() => onAction({ id: item.reservationId, action: 'confirm' })} disabled={isBusy}>
              Confirm
            </SmallAction>
          ) : null}
          {item.status === 2 ? (
            <SmallAction color="success" onClick={() => onAction({ id: item.reservationId, action: 'ready-for-pickup' })} disabled={isBusy}>
              Ready
            </SmallAction>
          ) : null}
          {item.status === 3 ? (
            <SmallAction color="success" onClick={() => onAction({ id: item.reservationId, action: 'complete' })} disabled={isBusy}>
              Complete
            </SmallAction>
          ) : null}
          {item.status === 1 || item.status === 2 || item.status === 3 ? (
            <SmallAction color="secondary" onClick={() => onAction({ id: item.reservationId, action: 'cancel' })}>
              Cancel
            </SmallAction>
          ) : null}
          {item.status === 1 || item.status === 2 ? (
            <SmallAction color="warning" onClick={() => onAction({ id: item.reservationId, action: 'expire' })} disabled={isBusy}>
              Expire
            </SmallAction>
          ) : null}
        </Stack>
      )}
      emptyLabel="No reservations found yet."
    />
  )
}
