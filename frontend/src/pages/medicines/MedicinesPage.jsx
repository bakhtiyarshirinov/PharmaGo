import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Button,
  Chip,
  Divider,
  Grid,
  InputAdornment,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import FavoriteBorderRoundedIcon from '@mui/icons-material/FavoriteBorderRounded'
import FavoriteRoundedIcon from '@mui/icons-material/FavoriteRounded'
import LocalOfferRoundedIcon from '@mui/icons-material/LocalOfferRounded'
import MedicationRoundedIcon from '@mui/icons-material/MedicationRounded'
import VerifiedRoundedIcon from '@mui/icons-material/VerifiedRounded'
import SearchRoundedIcon from '@mui/icons-material/SearchRounded'
import ShoppingBagRoundedIcon from '@mui/icons-material/ShoppingBagRounded'
import TuneRoundedIcon from '@mui/icons-material/TuneRounded'
import { useEffect, useMemo, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  addFavoriteMedicine,
  getFavoriteMedicines,
  getMedicineAvailability,
  getMedicineDetail,
  getMedicineSimilar,
  getMedicineSubstitutions,
  getRecentMedicines,
  removeFavoriteMedicine,
  searchMedicines,
} from '../../shared/api/medicines'
import { formatMoney, normalizeItems } from '../../shared/lib/format'
import { EmptyState } from '../../shared/ui/EmptyState'
import { EntityList, MetaChip, SmallAction } from '../../shared/ui/EntityList'
import { InfoCard } from '../../shared/ui/InfoCard'
import { SectionCard } from '../../shared/ui/SectionCard'
import { useSessionStore } from '../../features/auth/model/useSessionStore'
import { useAppUiStore } from '../../features/app/model/useAppUiStore'
import { HeroSurface } from '../../shared/ui/HeroSurface'

export function MedicinesPage() {
  const navigate = useNavigate()
  const { medicineId } = useParams()
  const session = useSessionStore((state) => state.session)
  const queryClient = useQueryClient()
  const setError = useAppUiStore((state) => state.setError)
  const setToast = useAppUiStore((state) => state.setToast)
  const [query, setQuery] = useState('Panadol')
  const [searchTerm, setSearchTerm] = useState('Panadol')

  const searchQuery = useQuery({
    queryKey: ['medicine-search', searchTerm],
    queryFn: () => searchMedicines(searchTerm),
    enabled: Boolean(searchTerm),
  })

  const medicineDetailQuery = useQuery({
    queryKey: ['medicine-detail', medicineId],
    queryFn: () => getMedicineDetail(medicineId),
    enabled: Boolean(medicineId),
  })

  const availabilityQuery = useQuery({
    queryKey: ['medicine-availability', medicineId],
    queryFn: () => getMedicineAvailability(medicineId),
    enabled: Boolean(medicineId),
  })

  const substitutionsQuery = useQuery({
    queryKey: ['medicine-substitutions', medicineId],
    queryFn: () => getMedicineSubstitutions(medicineId),
    enabled: Boolean(medicineId),
  })

  const similarQuery = useQuery({
    queryKey: ['medicine-similar', medicineId],
    queryFn: () => getMedicineSimilar(medicineId),
    enabled: Boolean(medicineId),
  })

  const favoritesQuery = useQuery({
    queryKey: ['me', 'medicines', 'favorites'],
    queryFn: getFavoriteMedicines,
    enabled: Boolean(session),
  })

  const recentQuery = useQuery({
    queryKey: ['me', 'medicines', 'recent'],
    queryFn: getRecentMedicines,
    enabled: Boolean(session),
  })

  const favoriteMutation = useMutation({
    mutationFn: ({ id, remove }) => (remove ? removeFavoriteMedicine(id) : addFavoriteMedicine(id)),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['me', 'medicines', 'favorites'] })
      queryClient.invalidateQueries({ queryKey: ['medicines', 'popular'] })
      setToast(variables.remove ? 'Medicine removed from favorites' : 'Medicine added to favorites')
    },
    onError: (error) => setError(error.message),
  })

  const results = normalizeItems(searchQuery.data)
  const selected = medicineDetailQuery.data
  const availability = normalizeItems(availabilityQuery.data)
  const substitutions = normalizeItems(substitutionsQuery.data)
  const similar = normalizeItems(similarQuery.data)
  const favorites = normalizeItems(favoritesQuery.data)
  const recent = normalizeItems(recentQuery.data)
  const favoriteIds = useMemo(() => new Set(favorites.map((item) => item.medicineId)), [favorites])
  const bestOffer = availability[0]

  useEffect(() => {
    if (!medicineId && results[0]?.medicineId) {
      navigate(`/medicines/${results[0].medicineId}`, { replace: true })
    }
  }, [medicineId, navigate, results])

  return (
    <Stack spacing={3}>
      <HeroSurface
        eyebrow={<MetaChip label="Medicine discovery" color="secondary" />}
        title="Каталог лекарств, который уже чувствуется как сервис, а не как список запросов."
        description="Поиск, availability, substitutions и similar medicines собраны в единый consumer screen: слева discovery, справа глубокая карточка товара и ближайшие аптеки."
        badges={[
          <MetaChip key="b1" label={`${results.length} results`} color="primary" />,
          <MetaChip key="b2" label={`${availability.length} pharmacy offers`} color="success" />,
          <MetaChip key="b3" label={`${favorites.length} favorites`} />,
        ]}
        metrics={[
          <Grid key="m1" size={{ xs: 6 }}>
            <InfoCard title="Popular query" value={searchTerm} />
          </Grid>,
          <Grid key="m2" size={{ xs: 6 }}>
            <InfoCard title="Cheapest visible" value={formatMoney(results[0]?.minRetailPrice)} />
          </Grid>,
        ]}
      />

      <Grid container spacing={3}>
        <Grid size={{ xs: 12, lg: 4 }}>
          <SectionCard
            title="Medicine explorer"
            subtitle="Geo-ready product discovery with substitutions and similar medicines"
            accent="secondary"
            tone="warm"
            actions={<MetaChip label="Search + compare" color="secondary" />}
          >
          <Stack spacing={2}>
            <TextField
              label="Search medicines"
              value={query}
              onChange={(event) => setQuery(event.target.value)}
              placeholder="Panadol, Nurofen, barcode, generic..."
              InputProps={{
                startAdornment: (
                  <InputAdornment position="start">
                    <SearchRoundedIcon />
                  </InputAdornment>
                ),
              }}
            />
            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.25}>
              <Button
                variant="contained"
                size="large"
                fullWidth
                onClick={() => {
                  setSearchTerm(query)
                }}
              >
              Search catalog
              </Button>
              <Button variant="outlined" size="large" startIcon={<TuneRoundedIcon />}>
                Filters
              </Button>
            </Stack>
            <Stack direction="row" spacing={1} flexWrap="wrap">
              {['Panadol', 'Nurofen', 'Theraflu', 'Claritin'].map((term) => (
                <Chip key={term} label={term} onClick={() => { setQuery(term); setSearchTerm(term) }} />
              ))}
            </Stack>
          </Stack>

          <Divider sx={{ my: 3 }} />

          <Stack direction="row" spacing={1} sx={{ mb: 2, flexWrap: 'wrap' }}>
            <MetaChip label={`${substitutions.length} substitutions`} color="secondary" />
            <MetaChip label={`${similar.length} similar`} color="primary" />
            <MetaChip label={selected?.requiresPrescription ? 'Prescription-aware' : 'OTC friendly'} color={selected?.requiresPrescription ? 'warning' : 'success'} />
          </Stack>

          <EntityList
            items={results}
            primaryKey="medicineId"
            titleKey="brandName"
            subtitleRenderer={(item) => `${item.genericName} • ${item.strength || 'No strength'} • ${formatMoney(item.minRetailPrice)}`}
            metaRenderer={(item) => (
              <>
                <MetaChip label={`${item.pharmacyCount || 0} pharmacies`} color="primary" />
                <MetaChip label={item.requiresPrescription ? 'Rx' : 'OTC'} color={item.requiresPrescription ? 'warning' : 'success'} />
              </>
            )}
            onItemClick={(item) => navigate(`/medicines/${item.medicineId}`)}
            emptyLabel="Use the search above to start exploring medicines."
          />
        </SectionCard>
        </Grid>

        <Grid size={{ xs: 12, lg: 8 }}>
          <Stack spacing={3}>
            <SectionCard
              title={selected?.brandName || 'Medicine detail'}
              subtitle={
                selected
                  ? `${selected.genericName} • ${selected.dosageForm || 'Form'} • ${selected.strength || 'Strength'}`
                  : 'Choose a medicine from the search results'
              }
              tone="cool"
              actions={
                selected ? (
                  <Stack direction="row" spacing={1}>
                    <MetaChip label={selected.requiresPrescription ? 'Prescription' : 'OTC'} color={selected.requiresPrescription ? 'warning' : 'success'} />
                    <MetaChip label={`${selected.pharmacyCount || 0} pharmacies`} color="primary" />
                  </Stack>
                ) : null
              }
            >
            {selected ? (
              <Stack spacing={2.5}>
                <Stack direction={{ xs: 'column', md: 'row' }} justifyContent="space-between" spacing={2}>
                  <Stack spacing={1}>
                    <Typography variant="h4">{selected.brandName}</Typography>
                    <Typography variant="body1" sx={{ color: 'text.secondary' }}>
                      {selected.description || 'No extended description yet. The card is ready for richer product copy and safety notes.'}
                    </Typography>
                  </Stack>
                  <Stack direction="row" spacing={1} flexWrap="wrap">
                    <Chip label={formatMoney(selected.minRetailPrice)} color="secondary" />
                    <Chip label={selected.categoryName || 'General medicine'} variant="outlined" />
                    <Chip label={selected.totalAvailableQuantity ? `${selected.totalAvailableQuantity} units live` : 'Low visibility'} />
                  </Stack>
                </Stack>

                <Grid container spacing={2}>
                  <Grid size={{ xs: 12, md: 4 }}>
                    <InfoCard title="Manufacturer" value={selected.manufacturer || 'Unknown'} />
                  </Grid>
                  <Grid size={{ xs: 12, md: 4 }}>
                    <InfoCard title="Country" value={selected.countryOfOrigin || 'Unknown'} />
                  </Grid>
                  <Grid size={{ xs: 12, md: 4 }}>
                    <InfoCard title="Available units" value={selected.totalAvailableQuantity ?? 0} />
                  </Grid>
                </Grid>

                {bestOffer ? (
                  <Grid container spacing={2}>
                    <Grid size={{ xs: 12, md: 7 }}>
                      <SectionCard
                        title="Best live offer"
                        subtitle={`${bestOffer.pharmacyName} • ${bestOffer.city || 'Unknown city'} • ${bestOffer.availableQuantity || 0} units right now`}
                        accent="secondary"
                        tone="warm"
                        actions={<MetaChip label={formatMoney(bestOffer.retailPrice)} color="secondary" />}
                      >
                        <Stack spacing={1.5}>
                          <Stack direction="row" spacing={1} flexWrap="wrap">
                            <MetaChip label={bestOffer.isOpenNow ? 'Open now' : 'Store profile'} color={bestOffer.isOpenNow ? 'success' : 'default'} />
                            <MetaChip label={bestOffer.supportsReservations ? 'Instant reservation' : 'View only'} color={bestOffer.supportsReservations ? 'primary' : 'default'} />
                            <MetaChip label={bestOffer.hasDelivery ? 'Delivery' : 'Pickup'} />
                          </Stack>
                          <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                            {bestOffer.address || 'Address unavailable'}
                          </Typography>
                        </Stack>
                      </SectionCard>
                    </Grid>
                    <Grid size={{ xs: 12, md: 5 }}>
                      <Stack spacing={2}>
                        <InfoCard title="Price edge" value={formatMoney(bestOffer.retailPrice)} />
                        <InfoCard title="Quick reserve" value={bestOffer.supportsReservations ? 'Available' : 'Unavailable'} />
                      </Stack>
                    </Grid>
                  </Grid>
                ) : null}

                <Stack direction={{ xs: 'column', md: 'row' }} spacing={1.5}>
                  {session ? (
                    <Button
                      variant="contained"
                      color={favoriteIds.has(selected.medicineId) ? 'secondary' : 'primary'}
                      startIcon={favoriteIds.has(selected.medicineId) ? <FavoriteRoundedIcon /> : <FavoriteBorderRoundedIcon />}
                      onClick={() => favoriteMutation.mutate({ id: selected.medicineId, remove: favoriteIds.has(selected.medicineId) })}
                    >
                      {favoriteIds.has(selected.medicineId) ? 'Remove favorite' : 'Save medicine'}
                    </Button>
                  ) : null}
                  <Button
                    variant="outlined"
                    startIcon={<ShoppingBagRoundedIcon />}
                    onClick={() => navigate('/reservations', { state: { medicineId: selected.medicineId, pharmacyId: availability[0]?.pharmacyId } })}
                  >
                    Start reservation flow
                  </Button>
                </Stack>
              </Stack>
            ) : (
              <EmptyState
                icon={<MedicationRoundedIcon fontSize="large" />}
                title="Pick a medicine"
                description="You will get the product card, live availability, substitutions and similar medicines here."
              />
            )}
          </SectionCard>

          <Grid container spacing={3}>
            <Grid size={{ xs: 12, xl: 6 }}>
              <SectionCard title="Availability" subtitle="Where the medicine is in stock right now" actions={<MetaChip label={`${availability.length} stores`} color="success" />} tone="cool">
                <EntityList
                  items={availability}
                  primaryKey="pharmacyId"
                  titleKey="pharmacyName"
                  subtitleRenderer={(item) => `${item.city || 'Unknown city'} • ${item.address || 'No address'} • ${item.availableQuantity || 0} units`}
                  metaRenderer={(item) => (
                    <>
                      <MetaChip label={formatMoney(item.retailPrice)} color="secondary" />
                      <MetaChip label={item.isOpenNow ? 'Open now' : 'Store details'} color={item.isOpenNow ? 'success' : 'default'} />
                      <MetaChip label={item.supportsReservations ? 'Reservable' : 'View only'} />
                    </>
                  )}
                  trailingRenderer={(item) => (
                    <SmallAction
                      onClick={() => navigate('/reservations', { state: { medicineId: selected?.medicineId, pharmacyId: item.pharmacyId } })}
                    >
                      Reserve
                    </SmallAction>
                  )}
                  emptyLabel="Availability will appear after you choose a medicine."
                />
              </SectionCard>
            </Grid>
            <Grid size={{ xs: 12, xl: 6 }}>
              <SectionCard
                title="Saved + recent"
                subtitle="Personal signals for the consumer medicine journey"
                actions={<MetaChip label={session ? 'Personalized' : 'Guest view'} color={session ? 'primary' : 'default'} />}
              >
                <EntityList
                  items={favorites.length ? favorites : recent}
                  primaryKey="medicineId"
                  titleKey="brandName"
                  subtitleRenderer={(item) =>
                    item.lastViewedAtUtc
                      ? `Viewed ${item.lastViewedAtUtc}`
                      : `${item.genericName || 'Medicine'} • ${formatMoney(item.minRetailPrice)}`
                  }
                  onItemClick={(item) => navigate(`/medicines/${item.medicineId}`)}
                  emptyLabel="Sign in and interact with medicines to see favorites or recent items."
                />
              </SectionCard>
            </Grid>
          </Grid>

          <Grid container spacing={3}>
            <Grid size={{ xs: 12, md: 6 }}>
              <SectionCard title="Substitutions" subtitle="Same generic, dosage form and strength" accent="secondary" tone="warm">
                <EntityList
                  items={substitutions}
                  primaryKey="medicineId"
                  titleKey="brandName"
                  subtitleRenderer={(item) => item.matchReason || `${item.genericName} • ${item.strength}`}
                  metaRenderer={(item) => (
                    <>
                      <MetaChip label={formatMoney(item.minRetailPrice)} color="secondary" />
                      <MetaChip label={item.hasAvailability ? 'Available' : 'Limited'} color={item.hasAvailability ? 'success' : 'default'} />
                      <MetaChip label="Safer switch" color="primary" />
                    </>
                  )}
                  onItemClick={(item) => navigate(`/medicines/${item.medicineId}`)}
                  emptyLabel="Substitutions will show up after medicine selection."
                />
              </SectionCard>
            </Grid>
            <Grid size={{ xs: 12, md: 6 }}>
              <SectionCard title="Similar medicines" subtitle="Related options for adjacent browsing" tone="cool">
                <EntityList
                  items={similar}
                  primaryKey="medicineId"
                  titleKey="brandName"
                  subtitleRenderer={(item) => item.matchReason || `${item.genericName} • ${item.dosageForm}`}
                  metaRenderer={(item) => (
                    <>
                      <MetaChip label={formatMoney(item.minRetailPrice)} color="secondary" />
                      <MetaChip label={`${item.pharmacyCount || 0} stores`} color="primary" />
                      <MetaChip label="Explore alternative" />
                    </>
                  )}
                  onItemClick={(item) => navigate(`/medicines/${item.medicineId}`)}
                  emptyLabel="Similar medicines will show up after medicine selection."
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
