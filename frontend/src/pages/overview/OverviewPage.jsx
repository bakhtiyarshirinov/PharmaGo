import { useQuery } from '@tanstack/react-query'
import { Button, Grid, Stack } from '@mui/material'
import MedicationRoundedIcon from '@mui/icons-material/MedicationRounded'
import MonitorHeartRoundedIcon from '@mui/icons-material/MonitorHeartRounded'
import NotificationsActiveRoundedIcon from '@mui/icons-material/NotificationsActiveRounded'
import StorefrontRoundedIcon from '@mui/icons-material/StorefrontRounded'
import ShoppingBagRoundedIcon from '@mui/icons-material/ShoppingBagRounded'
import { useNavigate } from 'react-router-dom'
import { getPopularMedicines } from '../../shared/api/medicines'
import { getPopularPharmacies } from '../../shared/api/pharmacies'
import { normalizeItems } from '../../shared/lib/format'
import { MetricBadge } from '../../shared/ui/MetricBadge'
import { MetricTile } from '../../shared/ui/MetricTile'
import { SectionCard } from '../../shared/ui/SectionCard'
import { EntityList, MetaChip } from '../../shared/ui/EntityList'
import { useSessionStore } from '../../features/auth/model/useSessionStore'
import { roleLabels } from '../../shared/config/roles'
import { HeroAction, HeroSurface } from '../../shared/ui/HeroSurface'
import '../../App.css'

export function OverviewPage() {
  const navigate = useNavigate()
  const session = useSessionStore((state) => state.session)
  const popularMedicinesQuery = useQuery({
    queryKey: ['medicines', 'popular', 6],
    queryFn: () => getPopularMedicines(6),
  })
  const popularPharmaciesQuery = useQuery({
    queryKey: ['pharmacies', 'popular', 6],
    queryFn: () => getPopularPharmacies(6),
  })

  const popularMedicines = normalizeItems(popularMedicinesQuery.data)
  const popularPharmacies = normalizeItems(popularPharmaciesQuery.data)
  const currentRole = session?.user?.role ? roleLabels[session.user.role] || 'User' : 'Guest'

  const metrics = [
    { label: 'Popular medicines', value: popularMedicines.length || '0', tone: 'default' },
    { label: 'Popular pharmacies', value: popularPharmacies.length || '0', tone: 'default' },
    { label: 'Profile', value: currentRole, tone: session ? 'success' : 'warning' },
    { label: 'Discovery surface', value: 'Live', tone: 'success' },
  ]

  return (
    <Stack spacing={3.5}>
      <HeroSurface
        eyebrow={<MetricBadge icon={<MonitorHeartRoundedIcon />} label="Pharmacy commerce interface" />}
        title="Проверять лекарства, находить ближайшие аптеки и жить в одном хорошем интерфейсе."
        description="Теперь это не просто тестовый стенд API, а нормальная продуктовая поверхность: discovery, карточки, favorites, reservations и staff visibility складываются в единый consumer journey."
        badges={[
          <MetricBadge key="m1" icon={<MedicationRoundedIcon />} label="Medicine discovery" />,
          <MetricBadge key="m2" icon={<StorefrontRoundedIcon />} label="Nearby pharmacy flow" />,
          <MetricBadge key="m3" icon={<NotificationsActiveRoundedIcon />} label="Reservation lifecycle" />,
        ]}
        actions={[
          <HeroAction key="a1" color="secondary" onClick={() => navigate('/medicines')}>
            Explore medicines
          </HeroAction>,
          <Button key="a2" size="large" variant="outlined" startIcon={<ShoppingBagRoundedIcon />} onClick={() => navigate('/reservations')}>
            Open reservations
          </Button>,
        ]}
        metrics={metrics.map((item) => (
          <Grid key={item.label} size={{ xs: 6 }}>
            <MetricTile item={item} />
          </Grid>
        ))}
      />

      <Grid container spacing={3}>
        <Grid size={{ xs: 12, lg: 6 }}>
          <SectionCard
            title="Popular medicines"
            subtitle="Fast entry points for the consumer medicine flow"
            accent="secondary"
            tone="warm"
            actions={<Button size="small" onClick={() => navigate('/medicines')}>View all</Button>}
          >
            <EntityList
              items={popularMedicines}
              primaryKey="medicineId"
              titleKey="brandName"
              subtitleRenderer={(item) => `${item.genericName} • ${item.strength || item.dosageForm || 'Medicine profile'}`}
              metaRenderer={(item) => (
                <>
                  <MetaChip label={`${item.pharmacyCount || 0} pharmacies`} color="primary" />
                  <MetaChip label={`${item.totalAvailableQuantity || 0} units`} />
                </>
              )}
              onItemClick={(item) => navigate(`/medicines/${item.medicineId}`)}
              emptyLabel="Popular medicines will show up here as soon as live demand data lands."
            />
          </SectionCard>
        </Grid>
        <Grid size={{ xs: 12, lg: 6 }}>
          <SectionCard
            title="Popular pharmacies"
            subtitle="High-signal stores surfaced from search, favorites and reservations"
            accent="primary"
            tone="cool"
            actions={<Button size="small" onClick={() => navigate('/pharmacies')}>View all</Button>}
          >
            <EntityList
              items={popularPharmacies}
              primaryKey="pharmacyId"
              titleKey="name"
              subtitleRenderer={(item) => `${item.city || 'City unknown'} • ${item.address || 'Address unavailable'}`}
              metaRenderer={(item) => (
                <>
                  <MetaChip label={item.isOpenNow ? 'Open now' : 'Store profile'} color={item.isOpenNow ? 'success' : 'default'} />
                  <MetaChip label={`${item.availableMedicineCount || 0} medicines`} />
                </>
              )}
              onItemClick={(item) => navigate(`/pharmacies/${item.pharmacyId}`)}
              emptyLabel="Popular pharmacies will show up here after discovery activity."
            />
          </SectionCard>
        </Grid>
      </Grid>
    </Stack>
  )
}
