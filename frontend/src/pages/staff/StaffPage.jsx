import { useQuery } from '@tanstack/react-query'
import { Grid, Paper, Stack, Typography, Chip } from '@mui/material'
import {
  getExpiringAlerts,
  getLowStockAlerts,
  getOutOfStockAlerts,
  getPharmacyStock,
  getRestockSuggestions,
} from '../../shared/api/stocks'
import { useSessionStore } from '../../features/auth/model/useSessionStore'
import { normalizeItems } from '../../shared/lib/format'
import { SectionCard } from '../../shared/ui/SectionCard'
import { EntityList } from '../../shared/ui/EntityList'
import '../../App.css'

export function StaffPage() {
  const session = useSessionStore((state) => state.session)
  const pharmacyId = session?.user?.pharmacyId

  const stockQuery = useQuery({
    queryKey: ['stocks', pharmacyId],
    queryFn: () => getPharmacyStock(pharmacyId),
    enabled: Boolean(pharmacyId),
  })
  const lowStockQuery = useQuery({ queryKey: ['stocks', 'low'], queryFn: getLowStockAlerts })
  const outOfStockQuery = useQuery({ queryKey: ['stocks', 'out'], queryFn: getOutOfStockAlerts })
  const expiringQuery = useQuery({ queryKey: ['stocks', 'expiring'], queryFn: () => getExpiringAlerts(30) })
  const restockQuery = useQuery({ queryKey: ['stocks', 'restock'], queryFn: getRestockSuggestions })

  const stock = normalizeItems(stockQuery.data)
  const lowStockAlerts = normalizeItems(lowStockQuery.data)
  const outOfStockAlerts = normalizeItems(outOfStockQuery.data)
  const expiringAlerts = normalizeItems(expiringQuery.data)
  const restockSuggestions = normalizeItems(restockQuery.data)

  return (
    <Stack spacing={3}>
      <Paper className="staff-banner" elevation={0}>
        <Stack direction={{ xs: 'column', md: 'row' }} justifyContent="space-between" spacing={2}>
          <Stack spacing={1}>
            <Typography variant="h3">Staff operations cockpit</Typography>
            <Typography sx={{ color: 'rgba(9, 20, 17, 0.74)', maxWidth: 760 }}>
              Этот экран опирается на уже существующие staff API и теперь лежит отдельно от consumer-страниц, а не внутри
              общего монолита.
            </Typography>
          </Stack>
          <Stack direction="row" spacing={1} flexWrap="wrap">
            <Chip label={`${stock.length} stock rows`} color="primary" />
            <Chip label={`${lowStockAlerts.length} low stock`} color="warning" />
            <Chip label={`${restockSuggestions.length} restock ideas`} color="secondary" />
          </Stack>
        </Stack>
      </Paper>

      <Grid container spacing={3}>
        <Grid size={{ xs: 12, lg: 6 }}>
          <SectionCard title="Low stock alerts" subtitle="Fast operational attention list">
            <EntityList
              items={lowStockAlerts}
              primaryKey="stockItemId"
              titleKey="medicineName"
              subtitleRenderer={(item) => `${item.pharmacyName || 'Pharmacy'} • ${item.availableQuantity || 0} units left`}
              emptyLabel="No low stock alerts were returned."
            />
          </SectionCard>
        </Grid>
        <Grid size={{ xs: 12, lg: 6 }}>
          <SectionCard title="Out of stock" subtitle="Availability gaps across pharmacies">
            <EntityList
              items={outOfStockAlerts}
              primaryKey="medicineId"
              titleKey="medicineName"
              subtitleRenderer={(item) => `${item.pharmacyName || 'Pharmacy'} • unavailable`}
              emptyLabel="No out-of-stock alerts were returned."
            />
          </SectionCard>
        </Grid>
        <Grid size={{ xs: 12, lg: 6 }}>
          <SectionCard title="Expiring soon" subtitle="Upcoming expiration pressure">
            <EntityList
              items={expiringAlerts}
              primaryKey="stockItemId"
              titleKey="medicineName"
              subtitleRenderer={(item) => `${item.pharmacyName || 'Pharmacy'} • expires ${item.expirationDate || 'soon'}`}
              emptyLabel="No expiring batches were returned."
            />
          </SectionCard>
        </Grid>
        <Grid size={{ xs: 12, lg: 6 }}>
          <SectionCard title="Restock suggestions" subtitle="Supplier-aware replenishment recommendations">
            <EntityList
              items={restockSuggestions}
              primaryKey="stockItemId"
              titleKey="medicineName"
              subtitleRenderer={(item) => `${item.supplierName || 'Supplier'} • suggested ${item.suggestedOrderQuantity || 0} units`}
              emptyLabel="No restock suggestions were returned."
            />
          </SectionCard>
        </Grid>
      </Grid>
    </Stack>
  )
}
