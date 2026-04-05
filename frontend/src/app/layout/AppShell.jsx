import {
  Alert,
  AppBar,
  Avatar,
  Box,
  Button,
  Chip,
  Container,
  IconButton,
  Snackbar,
  Stack,
  Toolbar,
  Tooltip,
  alpha,
} from '@mui/material'
import AutoAwesomeRoundedIcon from '@mui/icons-material/AutoAwesomeRounded'
import LocalHospitalRoundedIcon from '@mui/icons-material/LocalHospitalRounded'
import LoginRoundedIcon from '@mui/icons-material/LoginRounded'
import LogoutRoundedIcon from '@mui/icons-material/LogoutRounded'
import MedicationRoundedIcon from '@mui/icons-material/MedicationRounded'
import ShoppingBagRoundedIcon from '@mui/icons-material/ShoppingBagRounded'
import ShieldRoundedIcon from '@mui/icons-material/ShieldRounded'
import StorefrontRoundedIcon from '@mui/icons-material/StorefrontRounded'
import { NavLink, useLocation, useNavigate } from 'react-router-dom'
import { navigationItems } from '../../shared/config/navigation'
import { roleLabels } from '../../shared/config/roles'
import { useAppChrome } from '../../features/app/model/useAppChrome'
import { AuthDialog } from '../../features/auth/ui/AuthDialog'
import { getStatusTone } from '../../shared/lib/format'

export function AppShell({ children }) {
  const navigate = useNavigate()
  const location = useLocation()
  const {
    error,
    toast,
    setToast,
    setError,
    health,
    loginOpen,
    setLoginOpen,
    session,
    handleLogout,
  } = useAppChrome()

  const role = session?.user?.role
  const currentRole = role ? roleLabels[role] || 'User' : 'Guest'
  const isStaff = role === 2 || role === 3

  const icons = {
    overview: <AutoAwesomeRoundedIcon fontSize="small" />,
    medicines: <MedicationRoundedIcon fontSize="small" />,
    pharmacies: <StorefrontRoundedIcon fontSize="small" />,
    reservations: <ShoppingBagRoundedIcon fontSize="small" />,
    staff: <ShieldRoundedIcon fontSize="small" />,
  }

  const visibleNav = navigationItems.filter((item) => !item.staffOnly || isStaff)

  return (
    <Box sx={{ minHeight: '100vh' }}>
      <AppBar
        position="sticky"
        elevation={0}
        sx={{
          backdropFilter: 'blur(20px)',
          backgroundColor: alpha('#10231e', 0.68),
          borderBottom: `1px solid ${alpha('#ffffff', 0.08)}`,
        }}
      >
        <Toolbar sx={{ gap: 2, flexWrap: 'wrap', py: 1 }}>
          <Stack direction="row" spacing={1.5} alignItems="center" sx={{ flexGrow: 1 }}>
            <Avatar sx={{ bgcolor: 'secondary.main', color: '#fff', fontWeight: 700 }}>
              <LocalHospitalRoundedIcon />
            </Avatar>
            <Box>
              <Box component="div" sx={{ color: '#fff', fontWeight: 700 }}>
                PharmaGo
              </Box>
              <Box component="div" sx={{ color: alpha('#fff', 0.72), fontSize: 12 }}>
                consumer + staff platform
              </Box>
            </Box>
          </Stack>

          <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap' }}>
            {visibleNav.map((item) => (
              <Button
                key={item.to}
                component={NavLink}
                to={item.to}
                startIcon={icons[item.key]}
                sx={{
                  color: '#fff',
                  borderRadius: 999,
                  px: 2,
                  bgcolor: location.pathname === item.to || location.pathname.startsWith(`${item.to}/`) ? alpha('#fff', 0.14) : 'transparent',
                }}
              >
                {item.label}
              </Button>
            ))}
          </Stack>

          <Stack direction="row" spacing={1} alignItems="center">
            <Chip label={`${currentRole} • ${health}`} color={getStatusTone(health)} sx={{ fontWeight: 700 }} />
            {session ? (
              <Tooltip title="Logout">
                <IconButton
                  onClick={async () => {
                    await handleLogout()
                    navigate('/')
                  }}
                  sx={{ color: '#fff' }}
                >
                  <LogoutRoundedIcon />
                </IconButton>
              </Tooltip>
            ) : (
              <Button variant="contained" color="secondary" startIcon={<LoginRoundedIcon />} onClick={() => setLoginOpen(true)}>
                Sign in
              </Button>
            )}
          </Stack>
        </Toolbar>
      </AppBar>

      <Container maxWidth="xl" sx={{ py: 4 }}>
        {error ? (
          <Alert severity="error" sx={{ mb: 3, borderRadius: 4 }} onClose={() => setError('')}>
            {error}
          </Alert>
        ) : null}
        {children}
      </Container>

      <AuthDialog open={loginOpen} onClose={() => setLoginOpen(false)} />

      <Snackbar open={Boolean(toast)} autoHideDuration={2800} onClose={() => setToast('')}>
        <Alert onClose={() => setToast('')} severity="success" sx={{ width: '100%', borderRadius: 3 }}>
          {toast}
        </Alert>
      </Snackbar>
    </Box>
  )
}
