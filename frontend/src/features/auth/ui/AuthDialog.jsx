import { useMutation } from '@tanstack/react-query'
import {
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Grid,
  Stack,
  Tab,
  Tabs,
  TextField,
  Typography,
} from '@mui/material'
import { useState } from 'react'
import { login, register } from '../../../shared/api/auth'
import { useSessionStore } from '../model/useSessionStore'
import { useAppUiStore } from '../../app/model/useAppUiStore'

export function AuthDialog({ open, onClose }) {
  const setSession = useSessionStore((state) => state.setSession)
  const setError = useAppUiStore((state) => state.setError)
  const setToast = useAppUiStore((state) => state.setToast)
  const [tab, setTab] = useState('login')
  const [loginForm, setLoginForm] = useState({
    phoneNumber: '+994500000001',
    password: 'Pharmacist123!',
  })
  const [registerForm, setRegisterForm] = useState({
    firstName: 'Demo',
    lastName: 'User',
    phoneNumber: '+994551119999',
    email: 'demo.user@example.com',
    password: 'TestPassword123!',
  })

  const loginMutation = useMutation({
    mutationFn: login,
    onSuccess: (data) => {
      setSession(data)
      setToast('Signed in')
      onClose()
    },
    onError: (error) => setError(error.message),
  })

  const registerMutation = useMutation({
    mutationFn: register,
    onSuccess: (data) => {
      setSession(data)
      setToast('Account created')
      onClose()
    },
    onError: (error) => setError(error.message),
  })

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm" PaperProps={{ sx: { borderRadius: 6 } }}>
      <DialogTitle sx={{ pb: 1 }}>
        <Typography variant="h4">Welcome back to PharmaGo</Typography>
        <Typography variant="body2" sx={{ mt: 1, color: 'text.secondary' }}>
          Sign in for personalized consumer flows or staff-only inventory visibility.
        </Typography>
      </DialogTitle>
      <DialogContent>
        <Tabs value={tab} onChange={(_, value) => setTab(value)} sx={{ mb: 3 }}>
          <Tab value="login" label="Login" />
          <Tab value="register" label="Register" />
        </Tabs>

        {tab === 'login' ? (
          <BoxForm
            onSubmit={(event) => {
              event.preventDefault()
              loginMutation.mutate(loginForm)
            }}
          >
            <Stack spacing={2}>
              <TextField
                label="Phone number"
                value={loginForm.phoneNumber}
                onChange={(event) => setLoginForm((current) => ({ ...current, phoneNumber: event.target.value }))}
              />
              <TextField
                label="Password"
                type="password"
                value={loginForm.password}
                onChange={(event) => setLoginForm((current) => ({ ...current, password: event.target.value }))}
              />
              <Button type="submit" variant="contained" size="large" disabled={loginMutation.isPending}>
                {loginMutation.isPending ? 'Signing in...' : 'Sign in'}
              </Button>
            </Stack>
          </BoxForm>
        ) : (
          <BoxForm
            onSubmit={(event) => {
              event.preventDefault()
              registerMutation.mutate(registerForm)
            }}
          >
            <Stack spacing={2}>
              <Grid container spacing={2}>
                <Grid size={{ xs: 12, sm: 6 }}>
                  <TextField
                    label="First name"
                    value={registerForm.firstName}
                    onChange={(event) => setRegisterForm((current) => ({ ...current, firstName: event.target.value }))}
                    fullWidth
                  />
                </Grid>
                <Grid size={{ xs: 12, sm: 6 }}>
                  <TextField
                    label="Last name"
                    value={registerForm.lastName}
                    onChange={(event) => setRegisterForm((current) => ({ ...current, lastName: event.target.value }))}
                    fullWidth
                  />
                </Grid>
              </Grid>
              <TextField
                label="Phone number"
                value={registerForm.phoneNumber}
                onChange={(event) => setRegisterForm((current) => ({ ...current, phoneNumber: event.target.value }))}
              />
              <TextField
                label="Email"
                value={registerForm.email}
                onChange={(event) => setRegisterForm((current) => ({ ...current, email: event.target.value }))}
              />
              <TextField
                label="Password"
                type="password"
                value={registerForm.password}
                onChange={(event) => setRegisterForm((current) => ({ ...current, password: event.target.value }))}
              />
              <Button type="submit" variant="contained" size="large" color="secondary" disabled={registerMutation.isPending}>
                {registerMutation.isPending ? 'Creating...' : 'Create account'}
              </Button>
            </Stack>
          </BoxForm>
        )}
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 3 }}>
        <Button onClick={onClose}>Close</Button>
      </DialogActions>
    </Dialog>
  )
}

function BoxForm({ children, onSubmit }) {
  return (
    <form onSubmit={onSubmit}>
      {children}
    </form>
  )
}
