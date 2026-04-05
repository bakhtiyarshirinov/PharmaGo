import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Button, Chip, Divider, Grid, InputAdornment, Stack, TextField, Typography } from '@mui/material'
import FavoriteBorderRoundedIcon from '@mui/icons-material/FavoriteBorderRounded'
import FavoriteRoundedIcon from '@mui/icons-material/FavoriteRounded'
import ShoppingBagRoundedIcon from '@mui/icons-material/ShoppingBagRounded'
import StorefrontRoundedIcon from '@mui/icons-material/StorefrontRounded'
import AddLocationAltRoundedIcon from '@mui/icons-material/AddLocationAltRounded'
import DeliveryDiningRoundedIcon from '@mui/icons-material/DeliveryDiningRounded'
import VerifiedRoundedIcon from '@mui/icons-material/VerifiedRounded'
import { useEffect, useMemo, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  addFavoritePharmacy,
  getFavoritePharmacies,
  getPharmacyDetail,
  getPharmacyMedicines,
  getRecentPharmacies,
  removeFavoritePharmacy,
  searchPharmacies,
} from '../../shared/api/pharmacies'
import { formatDate, formatMoney, normalizeItems } from '../../shared/lib/format'
import { EntityList, MetaChip, SmallAction } from '../../shared/ui/EntityList'
import { EmptyState } from '../../shared/ui/EmptyState'
import { InfoCard } from '../../shared/ui/InfoCard'
import { SectionCard } from '../../shared/ui/SectionCard'
import { useSessionStore } from '../../features/auth/model/useSessionStore'
import { useAppUiStore } from '../../features/app/model/useAppUiStore'
import { HeroSurface } from '../../shared/ui/HeroSurface'

export function PharmaciesPage() {
  const navigate = useNavigate()
  const { pharmacyId } = useParams()
  const session = useSessionStore((state) => state.session)
  const queryClient = useQueryClient()
  const setError = useAppUiStore((state) => state.setError)
  const setToast = useAppUiStore((state) => state.setToast)
  const [query, setQuery] = useState('PharmaGo')
  const [searchTerm, setSearchTerm] = useState('PharmaGo')

  const searchQuery = useQuery({
    queryKey: ['pharmacy-search', searchTerm],
    queryFn: () => searchPharmacies(searchTerm),
    enabled: Boolean(searchTerm),
  })

  const detailQuery = useQuery({
    queryKey: ['pharmacy-detail', pharmacyId],
    queryFn: () => getPharmacyDetail(pharmacyId),
    enabled: Boolean(pharmacyId),
  })

  const catalogQuery = useQuery({
    queryKey: ['pharmacy-catalog', pharmacyId],
    queryFn: () => getPharmacyMedicines(pharmacyId),
    enabled: Boolean(pharmacyId),
  })

  const favoritesQuery = useQuery({
    queryKey: ['me', 'pharmacies', 'favorites'],
    queryFn: getFavoritePharmacies,
    enabled: Boolean(session),
  })

  const recentQuery = useQuery({
    queryKey: ['me', 'pharmacies', 'recent'],
    queryFn: getRecentPharmacies,
    enabled: Boolean(session),
  })

  const favoriteMutation = useMutation({
    mutationFn: ({ id, remove }) => (remove ? removeFavoritePharmacy(id) : addFavoritePharmacy(id)),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['me', 'pharmacies', 'favorites'] })
      queryClient.invalidateQueries({ queryKey: ['pharmacies', 'popular'] })
      setToast(variables.remove ? 'Pharmacy removed from favorites' : 'Pharmacy added to favorites')
    },
    onError: (error) => setError(error.message),
  })

  const results = normalizeItems(searchQuery.data)
  const selected = detailQuery.data
  const catalog = normalizeItems(catalogQuery.data)
  const favorites = normalizeItems(favoritesQuery.data)
  const recent = normalizeItems(recentQuery.data)
  const favoriteIds = useMemo(() => new Set(favorites.map((item) => item.pharmacyId)), [favorites])
  const topCatalogItem = catalog[0]

  useEffect(() => {
    if (!pharmacyId && results[0]?.pharmacyId) {
      navigate(`/pharmacies/${results[0].pharmacyId}`, { replace: true })
    }
  }, [navigate, pharmacyId, results])

  return (
    <Stack spacing={3}>
      <HeroSurface
        eyebrow={<MetaChip label="Pharmacy discovery" color="primary" />}
        title="Аптечный discovery flow с карточкой, каталогом и прямым переходом к бронированию."
        description="Теперь карточка аптеки больше не тупик: у каждой локации есть summary, услуги, price signals и pharmacy-centric medicine feed."
        badges={[
          <MetaChip key="p1" label={`${results.length} results`} color="primary" />,
          <MetaChip key="p2" label={`${catalog.length} catalog items`} color="secondary" />,
          <MetaChip key="p3" label={`${favorites.length} saved pharmacies`} />,
        ]}
        metrics={[
          <Grid key="pm1" size={{ xs: 6 }}>
            <InfoCard title="Search focus" value={searchTerm} />
          </Grid>,
          <Grid key="pm2" size={{ xs: 6 }}>
            <InfoCard title="Cheapest pharmacy" value={formatMoney(results[0]?.minAvailablePrice)} />
          </Grid>,
        ]}
      />

      <Grid container spacing={3}>
        <Grid size={{ xs: 12, lg: 4 }}>
          <SectionCard
            title="Pharmacy discovery"
            subtitle="Search nearby-ready stores, open the card and move directly into the catalog"
            tone="cool"
            actions={<MetaChip label="Search + open card" color="primary" />}
          >
          <Stack spacing={2}>
            <TextField
              label="Search pharmacies"
              value={query}
              onChange={(event) => setQuery(event.target.value)}
              placeholder="PharmaGo, Central, Baku..."
              InputProps={{
                startAdornment: (
                  <InputAdornment position="start">
                    <AddLocationAltRoundedIcon />
                  </InputAdornment>
                ),
              }}
            />
            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.25}>
              <Button variant="contained" size="large" fullWidth onClick={() => setSearchTerm(query)}>
                Search pharmacies
              </Button>
              <Button variant="outlined" size="large" startIcon={<DeliveryDiningRoundedIcon />}>
                Delivery
              </Button>
            </Stack>
            <Stack direction="row" spacing={1} flexWrap="wrap">
              {['PharmaGo', 'Central', 'Sahil', 'Sumqayit'].map((term) => (
                <Chip key={term} label={term} onClick={() => { setQuery(term); setSearchTerm(term) }} />
              ))}
            </Stack>
          </Stack>

          <Divider sx={{ my: 3 }} />

          <Stack direction="row" spacing={1} sx={{ mb: 2, flexWrap: 'wrap' }}>
            <MetaChip label={`${results.length} live stores`} color="primary" />
            <MetaChip label={results.some((item) => item.hasDelivery) ? 'Delivery available' : 'Pickup first'} color="secondary" />
            <MetaChip label="Open cards + browse catalog" />
          </Stack>

          <EntityList
            items={results}
            primaryKey="pharmacyId"
            titleKey="name"
            subtitleRenderer={(item) => `${item.city || 'Unknown city'} • ${item.address || 'No address'}`}
            metaRenderer={(item) => (
              <>
                <MetaChip label={item.isOpenNow ? 'Open now' : 'Store profile'} color={item.isOpenNow ? 'success' : 'default'} />
                <MetaChip label={`${item.availableMedicineCount || 0} medicines`} color="primary" />
              </>
            )}
            onItemClick={(item) => navigate(`/pharmacies/${item.pharmacyId}`)}
            emptyLabel="Use the search above to discover pharmacies."
          />
        </SectionCard>
        </Grid>

        <Grid size={{ xs: 12, lg: 8 }}>
          <Stack spacing={3}>
            <SectionCard
              title={selected?.name || 'Pharmacy card'}
              subtitle={selected ? `${selected.city || 'Unknown city'} • ${selected.address || 'No address'}` : 'Choose a pharmacy to open the full store card'}
              tone="cool"
              actions={
                selected ? (
                  <Stack direction="row" spacing={1}>
                    <MetaChip label={selected.isOpenNow ? 'Open now' : 'Store hours'} color={selected.isOpenNow ? 'success' : 'default'} />
                    <MetaChip label={selected.hasDelivery ? 'Delivery' : 'Pickup'} color={selected.hasDelivery ? 'secondary' : 'default'} />
                  </Stack>
                ) : null
              }
            >
            {selected ? (
              <Stack spacing={2.5}>
                <Stack direction={{ xs: 'column', md: 'row' }} justifyContent="space-between" spacing={2}>
                  <Stack spacing={1}>
                    <Typography variant="h4">{selected.name}</Typography>
                    <Typography variant="body1" sx={{ color: 'text.secondary' }}>
                      {selected.phoneNumber || 'Phone not attached'} • {selected.isOpen24Hours ? '24/7' : selected.isOpenNow ? 'Open now' : 'Store hours configured'}
                    </Typography>
                  </Stack>
                  <Stack direction="row" spacing={1} flexWrap="wrap">
                    <Chip label={`${selected.availableMedicineCount || selected.pharmacyCount || 0} active medicines`} variant="outlined" />
                    <Chip label={formatMoney(selected.minAvailablePrice)} color="secondary" />
                    <Chip label={selected.supportsReservations ? 'Reservations enabled' : 'View only'} />
                  </Stack>
                </Stack>

                <Grid container spacing={2}>
                  <Grid size={{ xs: 12, md: 4 }}>
                    <InfoCard title="Region" value={selected.region || 'Unknown'} />
                  </Grid>
                  <Grid size={{ xs: 12, md: 4 }}>
                    <InfoCard title="Min available price" value={formatMoney(selected.minAvailablePrice)} />
                  </Grid>
                  <Grid size={{ xs: 12, md: 4 }}>
                    <InfoCard title="Total stock units" value={selected.totalAvailableUnits ?? selected.totalAvailableQuantity ?? 0} />
                  </Grid>
                </Grid>

                <Grid container spacing={2}>
                  <Grid size={{ xs: 12, md: 8 }}>
                    <SectionCard
                      title="Store quality snapshot"
                      subtitle={selected.hasDelivery ? 'This pharmacy supports delivery and pickup scenarios.' : 'This pharmacy is optimized for pickup and in-store fulfillment.'}
                      tone="cool"
                      actions={<MetaChip label={selected.supportsReservations ? 'Reservations on' : 'Reservations off'} color={selected.supportsReservations ? 'primary' : 'default'} />}
                    >
                      <Stack direction="row" spacing={1} flexWrap="wrap">
                        <MetaChip label={selected.isOpenNow ? 'Open now' : 'Hours configured'} color={selected.isOpenNow ? 'success' : 'default'} />
                        <MetaChip label={selected.hasDelivery ? 'Delivery' : 'Pickup'} color="secondary" />
                        <MetaChip label="Verified location" color="primary" />
                      </Stack>
                    </SectionCard>
                  </Grid>
                  <Grid size={{ xs: 12, md: 4 }}>
                    <Stack spacing={2}>
                      <InfoCard title="Verified" value={selected.lastLocationVerifiedAtUtc ? 'Recently' : 'Pending'} />
                      <InfoCard title="Best visible price" value={formatMoney(selected.minAvailablePrice)} />
                    </Stack>
                  </Grid>
                </Grid>

                <Stack direction={{ xs: 'column', md: 'row' }} spacing={1.5}>
                  {session ? (
                    <Button
                      variant="contained"
                      color={favoriteIds.has(selected.pharmacyId) ? 'secondary' : 'primary'}
                      startIcon={favoriteIds.has(selected.pharmacyId) ? <FavoriteRoundedIcon /> : <FavoriteBorderRoundedIcon />}
                      onClick={() => favoriteMutation.mutate({ id: selected.pharmacyId, remove: favoriteIds.has(selected.pharmacyId) })}
                    >
                      {favoriteIds.has(selected.pharmacyId) ? 'Remove favorite' : 'Save pharmacy'}
                    </Button>
                  ) : null}
                  <Button
                    variant="outlined"
                    startIcon={<ShoppingBagRoundedIcon />}
                    onClick={() => navigate('/reservations', { state: { pharmacyId: selected.pharmacyId } })}
                  >
                    Start reservation here
                  </Button>
                </Stack>
              </Stack>
            ) : (
              <EmptyState
                icon={<StorefrontRoundedIcon fontSize="large" />}
                title="Pick a pharmacy"
                description="The card will show address, availability summary, services and the pharmacy-centric catalog flow."
              />
            )}
          </SectionCard>

          <Grid container spacing={3}>
            <Grid size={{ xs: 12, xl: 7 }}>
              <SectionCard
                title="Pharmacy catalog"
                subtitle="The pharmacy-centric browsing flow, no dead end cards"
                accent="secondary"
                tone="warm"
                actions={<MetaChip label={`${catalog.length} live items`} color="secondary" />}
              >
                {topCatalogItem ? (
                  <SectionCard
                    title="Highlighted shelf"
                    subtitle={`${topCatalogItem.brandName} • ${topCatalogItem.genericName || 'Medicine'} • ${topCatalogItem.availableQuantity || 0} units`}
                    tone="warm"
                    accent="secondary"
                    actions={<MetaChip label={formatMoney(topCatalogItem.retailPrice || topCatalogItem.minRetailPrice)} color="secondary" />}
                  >
                    <Stack direction="row" spacing={1} flexWrap="wrap">
                      <MetaChip label={topCatalogItem.isReservable ? 'Reservable' : 'View only'} color={topCatalogItem.isReservable ? 'success' : 'default'} />
                      <MetaChip label="Featured in this store" color="primary" />
                    </Stack>
                  </SectionCard>
                ) : null}

                <EntityList
                  items={catalog}
                  primaryKey="medicineId"
                  titleKey="brandName"
                  subtitleRenderer={(item) => `${item.genericName || 'Medicine'} • ${item.availableQuantity || 0} units • ${formatMoney(item.retailPrice || item.minRetailPrice)}`}
                  metaRenderer={(item) => (
                    <>
                      <MetaChip label={formatMoney(item.retailPrice || item.minRetailPrice)} color="secondary" />
                      <MetaChip label={item.isReservable ? 'Reservable' : 'View only'} color={item.isReservable ? 'success' : 'default'} />
                      <MetaChip label="In-store fit" color="primary" />
                    </>
                  )}
                  trailingRenderer={(item) => (
                    <SmallAction
                      onClick={() => navigate('/reservations', { state: { pharmacyId: selected?.pharmacyId, medicineId: item.medicineId } })}
                    >
                      Reserve
                    </SmallAction>
                  )}
                  onItemClick={(item) => navigate(`/medicines/${item.medicineId}`)}
                  emptyLabel="Open a pharmacy to browse its medicine catalog."
                />
              </SectionCard>
            </Grid>
            <Grid size={{ xs: 12, xl: 5 }}>
              <SectionCard
                title="Saved + recent"
                subtitle="Personalized pharmacy memory"
                actions={<MetaChip label={session ? 'Personalized' : 'Guest view'} color={session ? 'primary' : 'default'} />}
              >
                <EntityList
                  items={favorites.length ? favorites : recent}
                  primaryKey="pharmacyId"
                  titleKey="name"
                  subtitleRenderer={(item) =>
                    item.lastViewedAtUtc
                      ? `Viewed ${formatDate(item.lastViewedAtUtc)}`
                      : `${item.city || 'Unknown city'} • ${item.address || 'No address'}`
                  }
                  onItemClick={(item) => navigate(`/pharmacies/${item.pharmacyId}`)}
                  emptyLabel="Sign in and interact with pharmacies to populate this feed."
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
