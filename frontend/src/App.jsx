import { useEffect, useState } from 'react'
import './App.css'

const API_BASE = (import.meta.env.VITE_API_BASE_URL || '').replace(/\/$/, '')
const STORAGE_KEY = 'pharmago.frontend.session'

const roleLabels = {
  1: 'User',
  2: 'Pharmacist',
  3: 'Moderator',
}

const reservationStatusLabels = {
  1: 'Pending',
  2: 'Confirmed',
  3: 'Ready For Pickup',
  4: 'Completed',
  5: 'Cancelled',
  6: 'Expired',
}

function readStoredSession() {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    return raw ? JSON.parse(raw) : null
  } catch {
    return null
  }
}

function App() {
  const [session, setSession] = useState(() => readStoredSession())
  const [health, setHealth] = useState('checking')
  const [globalError, setGlobalError] = useState('')
  const [toast, setToast] = useState('')

  const [loginForm, setLoginForm] = useState({
    phoneNumber: '+994500000001',
    password: 'Pharmacist123!',
  })
  const [registerForm, setRegisterForm] = useState({
    firstName: 'Demo',
    lastName: 'User',
    phoneNumber: '+994551119999',
    email: `demo${Date.now()}@example.com`,
    password: 'TestPassword123!',
  })

  const [medicineQuery, setMedicineQuery] = useState('Panadol')
  const [medicineCity, setMedicineCity] = useState('Baku')
  const [medicineResults, setMedicineResults] = useState([])
  const [selectedMedicine, setSelectedMedicine] = useState(null)
  const [medicineAvailability, setMedicineAvailability] = useState([])
  const [medicineSubstitutions, setMedicineSubstitutions] = useState([])
  const [medicineSimilar, setMedicineSimilar] = useState([])
  const [popularMedicines, setPopularMedicines] = useState([])
  const [favoriteMedicines, setFavoriteMedicines] = useState([])
  const [recentMedicines, setRecentMedicines] = useState([])

  const [pharmacyQuery, setPharmacyQuery] = useState('PharmaGo')
  const [pharmacyCity, setPharmacyCity] = useState('Baku')
  const [pharmacyResults, setPharmacyResults] = useState([])
  const [selectedPharmacy, setSelectedPharmacy] = useState(null)
  const [pharmacyCatalog, setPharmacyCatalog] = useState([])
  const [popularPharmacies, setPopularPharmacies] = useState([])
  const [favoritePharmacies, setFavoritePharmacies] = useState([])
  const [recentPharmacies, setRecentPharmacies] = useState([])

  const [reservationForm, setReservationForm] = useState({
    pharmacyId: '',
    medicineId: '',
    quantity: 1,
    reserveForHours: 2,
    notes: '',
  })
  const [myReservations, setMyReservations] = useState([])
  const [activeReservations, setActiveReservations] = useState([])

  const [staffStock, setStaffStock] = useState([])
  const [lowStockAlerts, setLowStockAlerts] = useState([])
  const [outOfStockAlerts, setOutOfStockAlerts] = useState([])
  const [expiringAlerts, setExpiringAlerts] = useState([])
  const [restockSuggestions, setRestockSuggestions] = useState([])

  const roleName = session?.user?.role ? roleLabels[session.user.role] || `Role ${session.user.role}` : 'Guest'
  const isStaff = session?.user?.role === 2 || session?.user?.role === 3

  useEffect(() => {
    if (session) {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(session))
    } else {
      localStorage.removeItem(STORAGE_KEY)
    }
  }, [session])

  useEffect(() => {
    let cancelled = false

    async function checkHealth() {
      try {
        const response = await fetch(buildUrl('/health'))
        if (!cancelled) {
          setHealth(response.ok ? 'online' : 'degraded')
        }
      } catch {
        if (!cancelled) {
          setHealth('offline')
        }
      }
    }

    checkHealth()
    loadPopularMedicines()
    loadPopularPharmacies()

    return () => {
      cancelled = true
    }
  }, [])

  useEffect(() => {
    if (!session?.accessToken) {
      setFavoriteMedicines([])
      setRecentMedicines([])
      setFavoritePharmacies([])
      setRecentPharmacies([])
      setMyReservations([])
      setActiveReservations([])
      setStaffStock([])
      setLowStockAlerts([])
      setOutOfStockAlerts([])
      setExpiringAlerts([])
      setRestockSuggestions([])
      return
    }

    loadProfileScopedData()
  }, [session?.accessToken])

  useEffect(() => {
    if (!toast) {
      return undefined
    }

    const timeout = setTimeout(() => setToast(''), 2500)
    return () => clearTimeout(timeout)
  }, [toast])

  function buildUrl(path) {
    return `${API_BASE}${path}`
  }

  async function api(path, options = {}) {
    const headers = {
      ...(options.body ? { 'Content-Type': 'application/json' } : {}),
      ...(options.headers || {}),
    }

    if (session?.accessToken) {
      headers.Authorization = `Bearer ${session.accessToken}`
    }

    const response = await fetch(buildUrl(path), {
      ...options,
      headers,
    })

    const text = await response.text()
    let data = null

    if (text) {
      try {
        data = JSON.parse(text)
      } catch {
        data = text
      }
    }

    if (!response.ok) {
      const message =
        typeof data === 'string'
          ? data
          : data?.title || data?.detail || data?.message || `${response.status} ${response.statusText}`
      throw new Error(message)
    }

    return data
  }

  async function run(label, task, onSuccessMessage) {
    setGlobalError('')
    try {
      const result = await task()
      if (onSuccessMessage) {
        setToast(onSuccessMessage)
      }
      return result
    } catch (error) {
      setGlobalError(`${label}: ${error.message}`)
      return null
    }
  }

  async function loadProfileScopedData() {
    await Promise.all([
      loadFavoriteMedicines(),
      loadRecentMedicines(),
      loadFavoritePharmacies(),
      loadRecentPharmacies(),
      loadMyReservations(),
      loadActiveReservations(),
      isStaff ? loadStaffData() : Promise.resolve(),
    ])
  }

  async function loadStaffData() {
    await Promise.all([
      loadOwnStock(),
      loadLowStockAlerts(),
      loadOutOfStockAlerts(),
      loadExpiringAlerts(),
      loadRestockSuggestions(),
    ])
  }

  async function handleLogin(event) {
    event.preventDefault()
    const data = await run(
      'Login failed',
      () => api('/api/auth/login', { method: 'POST', body: JSON.stringify(loginForm) }),
      'Signed in',
    )

    if (data) {
      setSession(data)
    }
  }

  async function handleRegister(event) {
    event.preventDefault()
    const data = await run(
      'Registration failed',
      () => api('/api/auth/register', { method: 'POST', body: JSON.stringify(registerForm) }),
      'Account created',
    )

    if (data) {
      setSession(data)
    }
  }

  async function handleLogout() {
    if (!session?.refreshToken) {
      setSession(null)
      return
    }

    await run(
      'Logout failed',
      () =>
        api('/api/auth/logout', {
          method: 'POST',
          body: JSON.stringify({ refreshToken: session.refreshToken }),
        }),
      'Logged out',
    )
    setSession(null)
  }

  async function loadMe() {
    const data = await run('Profile request failed', () => api('/api/auth/me'))
    if (data && session) {
      setSession({ ...session, user: data })
    }
  }

  async function searchMedicines() {
    const params = new URLSearchParams()
    params.set('query', medicineQuery)
    if (medicineCity) {
      params.set('city', medicineCity)
    }

    const data = await run('Medicine search failed', () => api(`/api/medicines/search?${params.toString()}`))
    if (data) {
      setMedicineResults(data)
    }
  }

  async function loadPopularMedicines() {
    const data = await run('Popular medicines request failed', () => api('/api/medicines/popular?limit=8'))
    if (data) {
      setPopularMedicines(data)
    }
  }

  async function loadFavoriteMedicines() {
    const data = await run('Favorite medicines request failed', () => api('/api/me/medicines/favorites?limit=8'))
    if (data) {
      setFavoriteMedicines(data)
    }
  }

  async function loadRecentMedicines() {
    const data = await run('Recent medicines request failed', () => api('/api/me/medicines/recent?limit=8'))
    if (data) {
      setRecentMedicines(data)
    }
  }

  async function openMedicine(medicineId) {
    const detail = await run('Medicine detail failed', () => api(`/api/medicines/${medicineId}`))
    if (!detail) {
      return
    }

    setSelectedMedicine(detail)
    setReservationForm((current) => ({ ...current, medicineId }))

    const [availability, substitutions, similar] = await Promise.all([
      run('Medicine availability failed', () => api(`/api/medicines/${medicineId}/availability`)),
      run('Medicine substitutions failed', () => api(`/api/medicines/${medicineId}/substitutions?limit=6`)),
      run('Similar medicines failed', () => api(`/api/medicines/${medicineId}/similar?limit=6`)),
    ])

    if (availability) {
      setMedicineAvailability(availability.pharmacies || [])
    }
    if (substitutions) {
      setMedicineSubstitutions(substitutions)
    }
    if (similar) {
      setMedicineSimilar(similar)
    }

    if (session?.accessToken) {
      await loadRecentMedicines()
    }
  }

  async function toggleFavoriteMedicine(medicineId, isFavorite) {
    if (!session?.accessToken) {
      setGlobalError('Sign in first to manage favorite medicines.')
      return
    }

    const method = isFavorite ? 'DELETE' : 'POST'
    const label = isFavorite ? 'Remove favorite medicine failed' : 'Add favorite medicine failed'
    const success = isFavorite ? 'Medicine removed from favorites' : 'Medicine added to favorites'

    const data = await run(label, () => api(`/api/me/medicines/favorites/${medicineId}`, { method }), success)
    if (data !== null) {
      await Promise.all([loadFavoriteMedicines(), loadPopularMedicines()])
    }
  }

  async function searchPharmacies() {
    const params = new URLSearchParams()
    if (pharmacyQuery) {
      params.set('query', pharmacyQuery)
    }
    if (pharmacyCity) {
      params.set('city', pharmacyCity)
    }

    const data = await run('Pharmacy search failed', () => api(`/api/pharmacies/search?${params.toString()}`))
    if (data) {
      setPharmacyResults(data.items || [])
    }
  }

  async function loadPopularPharmacies() {
    const data = await run('Popular pharmacies request failed', () => api('/api/pharmacies/popular?limit=8'))
    if (data) {
      setPopularPharmacies(data)
    }
  }

  async function loadFavoritePharmacies() {
    const data = await run('Favorite pharmacies request failed', () => api('/api/me/pharmacies/favorites?limit=8'))
    if (data) {
      setFavoritePharmacies(data)
    }
  }

  async function loadRecentPharmacies() {
    const data = await run('Recent pharmacies request failed', () => api('/api/me/pharmacies/recent?limit=8'))
    if (data) {
      setRecentPharmacies(data)
    }
  }

  async function openPharmacy(pharmacyId) {
    const detail = await run('Pharmacy detail failed', () => api(`/api/pharmacies/${pharmacyId}`))
    if (!detail) {
      return
    }

    setSelectedPharmacy(detail)
    setReservationForm((current) => ({ ...current, pharmacyId }))

    const catalog = await run('Pharmacy catalog failed', () => api(`/api/pharmacies/${pharmacyId}/medicines?pageSize=8`))
    if (catalog) {
      setPharmacyCatalog(catalog.items || [])
    }

    if (session?.accessToken) {
      await loadRecentPharmacies()
    }
  }

  async function toggleFavoritePharmacy(pharmacyId, isFavorite) {
    if (!session?.accessToken) {
      setGlobalError('Sign in first to manage favorite pharmacies.')
      return
    }

    const method = isFavorite ? 'DELETE' : 'POST'
    const label = isFavorite ? 'Remove favorite pharmacy failed' : 'Add favorite pharmacy failed'
    const success = isFavorite ? 'Pharmacy removed from favorites' : 'Pharmacy added to favorites'

    const data = await run(label, () => api(`/api/me/pharmacies/favorites/${pharmacyId}`, { method }), success)
    if (data !== null) {
      await Promise.all([loadFavoritePharmacies(), loadPopularPharmacies()])
    }
  }

  async function createReservation(event) {
    event.preventDefault()

    const payload = {
      pharmacyId: reservationForm.pharmacyId,
      reserveForHours: Number(reservationForm.reserveForHours),
      notes: reservationForm.notes || null,
      items: [
        {
          medicineId: reservationForm.medicineId,
          quantity: Number(reservationForm.quantity),
        },
      ],
    }

    const data = await run(
      'Reservation creation failed',
      () => api('/api/reservations', { method: 'POST', body: JSON.stringify(payload) }),
      'Reservation created',
    )

    if (data) {
      await Promise.all([loadMyReservations(), loadActiveReservations()])
    }
  }

  async function loadMyReservations() {
    const data = await run('My reservations request failed', () => api('/api/reservations/my'))
    if (data) {
      setMyReservations(data)
    }
  }

  async function loadActiveReservations() {
    const data = await run('Active reservations request failed', () => api('/api/reservations/active'))
    if (data) {
      setActiveReservations(data)
    }
  }

  async function changeReservationStatus(reservationId, action) {
    const data = await run(
      `Reservation ${action} failed`,
      () => api(`/api/reservations/${reservationId}/${action}`, { method: 'POST' }),
      `Reservation ${action} executed`,
    )

    if (data) {
      await Promise.all([loadMyReservations(), loadActiveReservations()])
    }
  }

  async function loadOwnStock() {
    if (!session?.user?.pharmacyId) {
      return
    }

    const data = await run(
      'Stock list request failed',
      () => api(`/api/stocks/pharmacy/${session.user.pharmacyId}`),
    )
    if (data) {
      setStaffStock(data)
    }
  }

  async function loadLowStockAlerts() {
    const data = await run('Low-stock request failed', () => api('/api/stocks/alerts/low-stock'))
    if (data) {
      setLowStockAlerts(data)
    }
  }

  async function loadOutOfStockAlerts() {
    const data = await run('Out-of-stock request failed', () => api('/api/stocks/alerts/out-of-stock'))
    if (data) {
      setOutOfStockAlerts(data)
    }
  }

  async function loadExpiringAlerts() {
    const data = await run('Expiring stock request failed', () => api('/api/stocks/alerts/expiring?days=30'))
    if (data) {
      setExpiringAlerts(data)
    }
  }

  async function loadRestockSuggestions() {
    const data = await run('Restock suggestions request failed', () => api('/api/stocks/alerts/restock-suggestions'))
    if (data) {
      setRestockSuggestions(data)
    }
  }

  function pickReservation(pharmacyId, medicineId) {
    setReservationForm((current) => ({
      ...current,
      pharmacyId,
      medicineId,
    }))
    setToast('Reservation form prefilled')
  }

  return (
    <div className="app-shell">
      <header className="hero-shell">
        <div>
          <p className="eyebrow">PharmaGo Testbed</p>
          <h1>Minimal frontend to test backend flows end-to-end</h1>
          <p className="lede">
            One screen for auth, consumer medicine and pharmacy flows, reservations and staff inventory tools.
          </p>
        </div>
        <div className="status-grid">
          <MetricCard label="Backend" value={health} tone={health === 'online' ? 'good' : health === 'offline' ? 'bad' : 'warn'} />
          <MetricCard label="API Base" value={API_BASE || 'vite proxy → localhost:5122'} />
          <MetricCard label="Role" value={roleName} />
          <MetricCard label="Session" value={session?.user?.phoneNumber || 'Anonymous'} />
        </div>
      </header>

      {globalError ? <div className="banner error">{globalError}</div> : null}
      {toast ? <div className="banner success">{toast}</div> : null}

      <main className="grid">
        <section className="panel">
          <PanelHeading
            title="Auth"
            subtitle="Sign in as user, pharmacist or moderator. Session is persisted in localStorage."
          />

          <div className="split">
            <form className="stack" onSubmit={handleLogin}>
              <h3>Login</h3>
              <label>
                Phone
                <input
                  value={loginForm.phoneNumber}
                  onChange={(event) => setLoginForm({ ...loginForm, phoneNumber: event.target.value })}
                />
              </label>
              <label>
                Password
                <input
                  type="password"
                  value={loginForm.password}
                  onChange={(event) => setLoginForm({ ...loginForm, password: event.target.value })}
                />
              </label>
              <button type="submit">Login</button>
            </form>

            <form className="stack" onSubmit={handleRegister}>
              <h3>Register</h3>
              <label>
                First name
                <input
                  value={registerForm.firstName}
                  onChange={(event) => setRegisterForm({ ...registerForm, firstName: event.target.value })}
                />
              </label>
              <label>
                Last name
                <input
                  value={registerForm.lastName}
                  onChange={(event) => setRegisterForm({ ...registerForm, lastName: event.target.value })}
                />
              </label>
              <label>
                Phone
                <input
                  value={registerForm.phoneNumber}
                  onChange={(event) => setRegisterForm({ ...registerForm, phoneNumber: event.target.value })}
                />
              </label>
              <label>
                Email
                <input
                  value={registerForm.email}
                  onChange={(event) => setRegisterForm({ ...registerForm, email: event.target.value })}
                />
              </label>
              <label>
                Password
                <input
                  type="password"
                  value={registerForm.password}
                  onChange={(event) => setRegisterForm({ ...registerForm, password: event.target.value })}
                />
              </label>
              <button type="submit">Register</button>
            </form>
          </div>

          <div className="toolbar">
            <button onClick={loadMe} disabled={!session}>Refresh `/me`</button>
            <button onClick={handleLogout}>Logout</button>
          </div>

          <JsonBlock title="Session" data={session} />
        </section>

        <section className="panel">
          <PanelHeading
            title="Medicines"
            subtitle="Search, detail, availability, substitutions, similar, popular, favorites and recent."
          />

          <div className="toolbar">
            <button onClick={loadPopularMedicines}>Popular</button>
            <button onClick={loadFavoriteMedicines} disabled={!session}>My Favorites</button>
            <button onClick={loadRecentMedicines} disabled={!session}>Recent</button>
          </div>

          <form
            className="inline-form"
            onSubmit={(event) => {
              event.preventDefault()
              searchMedicines()
            }}
          >
            <input value={medicineQuery} onChange={(event) => setMedicineQuery(event.target.value)} placeholder="Search medicines" />
            <input value={medicineCity} onChange={(event) => setMedicineCity(event.target.value)} placeholder="City" />
            <button type="submit">Search</button>
          </form>

          <div className="split results-grid">
            <FeedBlock
              title="Search Results"
              items={medicineResults}
              renderItem={(item) => (
                <ItemCard
                  key={item.medicineId}
                  title={item.brandName}
                  meta={`${item.genericName} · ${item.dosageForm} · ${item.strength}`}
                  badges={[
                    item.requiresPrescription ? 'Rx' : 'OTC',
                    item.pharmacyCount ? `${item.pharmacyCount} pharmacies` : null,
                    item.minRetailPrice ? `${item.minRetailPrice} AZN` : null,
                  ]}
                  actions={
                    <>
                      <button onClick={() => openMedicine(item.medicineId)}>Open</button>
                      <button onClick={() => toggleFavoriteMedicine(item.medicineId, item.isFavorite)} disabled={!session}>
                        {item.isFavorite ? 'Unfavorite' : 'Favorite'}
                      </button>
                    </>
                  }
                />
              )}
            />

            <FeedBlock
              title="Popular / Favorite / Recent"
              items={[...popularMedicines, ...favoriteMedicines, ...recentMedicines].slice(0, 12)}
              renderItem={(item, index) => (
                <ItemCard
                  key={`${item.medicineId}-${index}`}
                  title={item.brandName}
                  meta={`${item.genericName} · ${item.strength}`}
                  badges={[
                    item.isFavorite ? 'Favorite' : null,
                    item.popularityScore ? `Score ${item.popularityScore}` : null,
                    item.lastViewedAtUtc ? 'Recent' : null,
                  ]}
                  actions={<button onClick={() => openMedicine(item.medicineId)}>Open</button>}
                />
              )}
            />
          </div>

          <DetailCard
            title="Selected Medicine"
            emptyText="Open a medicine card to inspect detail, availability and recommendation flows."
            data={selectedMedicine}
          >
            {selectedMedicine ? (
              <>
                <div className="metric-row">
                  <Metric label="Brand" value={selectedMedicine.brandName} />
                  <Metric label="Generic" value={selectedMedicine.genericName} />
                  <Metric label="Strength" value={selectedMedicine.strength} />
                  <Metric label="Min Price" value={selectedMedicine.minRetailPrice || '—'} />
                </div>
                <Subsection
                  title="Availability"
                  items={medicineAvailability}
                  renderItem={(item) => (
                    <CompactRow
                      key={item.pharmacyId}
                      title={item.pharmacyName}
                      meta={`${item.availableQuantity} units · ${item.retailPrice} AZN`}
                      buttonLabel="Use in reservation"
                      onAction={() => pickReservation(item.pharmacyId, selectedMedicine.medicineId)}
                    />
                  )}
                />
                <Subsection
                  title="Substitutions"
                  items={medicineSubstitutions}
                  renderItem={(item) => (
                    <CompactRow
                      key={item.medicineId}
                      title={item.brandName}
                      meta={item.matchReason}
                      buttonLabel="Open"
                      onAction={() => openMedicine(item.medicineId)}
                    />
                  )}
                />
                <Subsection
                  title="Similar"
                  items={medicineSimilar}
                  renderItem={(item) => (
                    <CompactRow
                      key={item.medicineId}
                      title={item.brandName}
                      meta={item.matchReason}
                      buttonLabel="Open"
                      onAction={() => openMedicine(item.medicineId)}
                    />
                  )}
                />
              </>
            ) : null}
          </DetailCard>
        </section>

        <section className="panel">
          <PanelHeading
            title="Pharmacies"
            subtitle="Search, detail, catalog, popular, favorites and recent pharmacy flows."
          />

          <div className="toolbar">
            <button onClick={loadPopularPharmacies}>Popular</button>
            <button onClick={loadFavoritePharmacies} disabled={!session}>My Favorites</button>
            <button onClick={loadRecentPharmacies} disabled={!session}>Recent</button>
          </div>

          <form
            className="inline-form"
            onSubmit={(event) => {
              event.preventDefault()
              searchPharmacies()
            }}
          >
            <input value={pharmacyQuery} onChange={(event) => setPharmacyQuery(event.target.value)} placeholder="Search pharmacies" />
            <input value={pharmacyCity} onChange={(event) => setPharmacyCity(event.target.value)} placeholder="City" />
            <button type="submit">Search</button>
          </form>

          <div className="split results-grid">
            <FeedBlock
              title="Search Results"
              items={pharmacyResults}
              renderItem={(item) => (
                <ItemCard
                  key={item.pharmacyId}
                  title={item.name}
                  meta={`${item.city}${item.region ? ` · ${item.region}` : ''}`}
                  badges={[
                    item.isOpenNow ? 'Open now' : null,
                    item.hasDelivery ? 'Delivery' : null,
                    item.supportsReservations ? 'Reservations' : null,
                  ]}
                  actions={
                    <>
                      <button onClick={() => openPharmacy(item.pharmacyId)}>Open</button>
                      <button onClick={() => toggleFavoritePharmacy(item.pharmacyId, item.isFavorite)} disabled={!session}>
                        {item.isFavorite ? 'Unfavorite' : 'Favorite'}
                      </button>
                    </>
                  }
                />
              )}
            />

            <FeedBlock
              title="Popular / Favorite / Recent"
              items={[...popularPharmacies, ...favoritePharmacies, ...recentPharmacies].slice(0, 12)}
              renderItem={(item, index) => (
                <ItemCard
                  key={`${item.pharmacyId}-${index}`}
                  title={item.name}
                  meta={`${item.city}${item.region ? ` · ${item.region}` : ''}`}
                  badges={[
                    item.isFavorite ? 'Favorite' : null,
                    item.popularityScore ? `Score ${item.popularityScore}` : null,
                    item.lastViewedAtUtc ? 'Recent' : null,
                  ]}
                  actions={<button onClick={() => openPharmacy(item.pharmacyId)}>Open</button>}
                />
              )}
            />
          </div>

          <DetailCard
            title="Selected Pharmacy"
            emptyText="Open a pharmacy card to inspect detail and browse medicines inside that pharmacy."
            data={selectedPharmacy}
          >
            {selectedPharmacy ? (
              <>
                <div className="metric-row">
                  <Metric label="Name" value={selectedPharmacy.name} />
                  <Metric label="City" value={selectedPharmacy.city} />
                  <Metric label="Medicines" value={selectedPharmacy.availableMedicineCount} />
                  <Metric label="Min Price" value={selectedPharmacy.minAvailablePrice || '—'} />
                </div>
                <Subsection
                  title="Pharmacy Catalog"
                  items={pharmacyCatalog}
                  renderItem={(item) => (
                    <CompactRow
                      key={item.medicineId}
                      title={item.brandName}
                      meta={`${item.availableQuantity} units · ${item.minRetailPrice} AZN`}
                      buttonLabel="Use in reservation"
                      onAction={() => pickReservation(selectedPharmacy.pharmacyId, item.medicineId)}
                    />
                  )}
                />
              </>
            ) : null}
          </DetailCard>
        </section>

        <section className="panel">
          <PanelHeading
            title="Reservations"
            subtitle="Create reservations from selected medicine/pharmacy pairs and move them through lifecycle commands."
          />

          <form className="stack" onSubmit={createReservation}>
            <div className="split compact">
              <label>
                Pharmacy ID
                <input
                  value={reservationForm.pharmacyId}
                  onChange={(event) => setReservationForm({ ...reservationForm, pharmacyId: event.target.value })}
                />
              </label>
              <label>
                Medicine ID
                <input
                  value={reservationForm.medicineId}
                  onChange={(event) => setReservationForm({ ...reservationForm, medicineId: event.target.value })}
                />
              </label>
            </div>

            <div className="split compact">
              <label>
                Quantity
                <input
                  type="number"
                  min="1"
                  value={reservationForm.quantity}
                  onChange={(event) => setReservationForm({ ...reservationForm, quantity: event.target.value })}
                />
              </label>
              <label>
                Reserve hours
                <input
                  type="number"
                  min="1"
                  max="24"
                  value={reservationForm.reserveForHours}
                  onChange={(event) => setReservationForm({ ...reservationForm, reserveForHours: event.target.value })}
                />
              </label>
            </div>

            <label>
              Notes
              <textarea
                rows="3"
                value={reservationForm.notes}
                onChange={(event) => setReservationForm({ ...reservationForm, notes: event.target.value })}
              />
            </label>

            <div className="toolbar">
              <button type="submit" disabled={!session}>Create Reservation</button>
              <button type="button" onClick={loadMyReservations} disabled={!session}>My Reservations</button>
              <button type="button" onClick={loadActiveReservations} disabled={!session}>Active</button>
            </div>
          </form>

          <div className="split results-grid">
            <FeedBlock
              title="My Reservations"
              items={myReservations}
              renderItem={(item) => (
                <ItemCard
                  key={item.reservationId}
                  title={item.reservationNumber}
                  meta={`${reservationStatusLabels[item.status] || item.status} · ${item.pharmacyName}`}
                  badges={[`${item.totalAmount} AZN`, new Date(item.reservedUntilUtc).toLocaleString()]}
                  actions={
                    <>
                      {item.status === 2 || item.status === 3 || item.status === 1 ? (
                        <button onClick={() => changeReservationStatus(item.reservationId, 'cancel')}>Cancel</button>
                      ) : null}
                    </>
                  }
                />
              )}
            />

            <FeedBlock
              title="Active / Staff Actions"
              items={activeReservations}
              renderItem={(item) => (
                <ItemCard
                  key={item.reservationId}
                  title={item.reservationNumber}
                  meta={`${reservationStatusLabels[item.status] || item.status} · ${item.customerFullName || item.pharmacyName}`}
                  badges={[item.pharmacyName]}
                  actions={
                    <>
                      {isStaff && item.status === 2 ? (
                        <button onClick={() => changeReservationStatus(item.reservationId, 'ready-for-pickup')}>Ready</button>
                      ) : null}
                      {isStaff && item.status === 3 ? (
                        <button onClick={() => changeReservationStatus(item.reservationId, 'complete')}>Complete</button>
                      ) : null}
                      {isStaff && (item.status === 2 || item.status === 3 || item.status === 1) ? (
                        <button onClick={() => changeReservationStatus(item.reservationId, 'expire')}>Expire</button>
                      ) : null}
                    </>
                  }
                />
              )}
            />
          </div>
        </section>

        {isStaff ? (
          <section className="panel">
            <PanelHeading
              title="Staff Tools"
              subtitle="Low-stock, out-of-stock, expiring and restock endpoints in one place for pharmacist or moderator testing."
            />

            <div className="toolbar wrap">
              <button onClick={loadOwnStock}>Stock List</button>
              <button onClick={loadLowStockAlerts}>Low Stock</button>
              <button onClick={loadOutOfStockAlerts}>Out Of Stock</button>
              <button onClick={loadExpiringAlerts}>Expiring</button>
              <button onClick={loadRestockSuggestions}>Restock Suggestions</button>
            </div>

            <div className="split results-grid">
              <FeedBlock
                title="Own Stock"
                items={staffStock}
                renderItem={(item) => (
                  <ItemCard
                    key={item.id}
                    title={item.medicineName}
                    meta={`${item.batchNumber} · ${item.availableQuantity} available`}
                    badges={[item.isLowStock ? 'Low stock' : null, `${item.retailPrice} AZN`]}
                  />
                )}
              />
              <FeedBlock
                title="Alerts"
                items={[...lowStockAlerts, ...outOfStockAlerts, ...expiringAlerts, ...restockSuggestions].slice(0, 16)}
                renderItem={(item, index) => (
                  <ItemCard
                    key={`${item.stockItemId || item.medicineId || index}-${index}`}
                    title={item.medicineName || item.name || 'Alert'}
                    meta={item.pharmacyName || item.depotName || item.batchNumber || ''}
                    badges={[
                      item.deficit ? `Deficit ${item.deficit}` : null,
                      item.daysUntilExpiration !== undefined ? `${item.daysUntilExpiration} days` : null,
                      item.suggestedOrderQuantity ? `Order ${item.suggestedOrderQuantity}` : null,
                    ]}
                  />
                )}
              />
            </div>
          </section>
        ) : null}
      </main>
    </div>
  )
}

function PanelHeading({ title, subtitle }) {
  return (
    <div className="panel-heading">
      <h2>{title}</h2>
      <p>{subtitle}</p>
    </div>
  )
}

function MetricCard({ label, value, tone = 'neutral' }) {
  return (
    <div className={`metric-card ${tone}`}>
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  )
}

function FeedBlock({ title, items, renderItem }) {
  return (
    <div className="feed-block">
      <div className="feed-header">
        <h3>{title}</h3>
        <span>{items.length}</span>
      </div>
      <div className="feed-list">
        {items.length ? items.map(renderItem) : <div className="empty-box">No data loaded yet.</div>}
      </div>
    </div>
  )
}

function ItemCard({ title, meta, badges = [], actions }) {
  return (
    <article className="item-card">
      <div>
        <h4>{title}</h4>
        {meta ? <p>{meta}</p> : null}
        <div className="badge-row">
          {badges.filter(Boolean).map((badge) => (
            <span className="badge" key={badge}>
              {badge}
            </span>
          ))}
        </div>
      </div>
      {actions ? <div className="card-actions">{actions}</div> : null}
    </article>
  )
}

function DetailCard({ title, emptyText, data, children }) {
  return (
    <div className="detail-card">
      <div className="feed-header">
        <h3>{title}</h3>
        <span>{data ? 'Loaded' : 'Empty'}</span>
      </div>
      {data ? children : <div className="empty-box">{emptyText}</div>}
    </div>
  )
}

function Metric({ label, value }) {
  return (
    <div className="metric">
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  )
}

function Subsection({ title, items, renderItem }) {
  return (
    <div className="subsection">
      <div className="feed-header">
        <h3>{title}</h3>
        <span>{items.length}</span>
      </div>
      {items.length ? items.map(renderItem) : <div className="empty-box">No items.</div>}
    </div>
  )
}

function CompactRow({ title, meta, buttonLabel, onAction }) {
  return (
    <div className="compact-row">
      <div>
        <strong>{title}</strong>
        <p>{meta}</p>
      </div>
      <button onClick={onAction}>{buttonLabel}</button>
    </div>
  )
}

function JsonBlock({ title, data }) {
  return (
    <div className="json-block">
      <div className="feed-header">
        <h3>{title}</h3>
      </div>
      <pre>{JSON.stringify(data, null, 2)}</pre>
    </div>
  )
}

export default App
