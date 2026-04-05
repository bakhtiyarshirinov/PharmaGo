import { createTheme } from '@mui/material'

export const appTheme = createTheme({
  palette: {
    primary: {
      main: '#1f7a65',
      light: '#46a38a',
      dark: '#115544',
    },
    secondary: {
      main: '#ef6c3f',
      light: '#ff9d78',
      dark: '#c14d24',
    },
    background: {
      default: '#f5f1e8',
      paper: 'rgba(255,255,255,0.78)',
    },
    success: {
      main: '#2f8f5b',
    },
    warning: {
      main: '#cf8a2b',
    },
    error: {
      main: '#c84f44',
    },
  },
  shape: {
    borderRadius: 22,
  },
  typography: {
    fontFamily: '"Space Grotesk", "DM Sans", "Segoe UI", sans-serif',
    h1: {
      fontWeight: 700,
      letterSpacing: '-0.06em',
    },
    h2: {
      fontWeight: 700,
      letterSpacing: '-0.04em',
    },
    h3: {
      fontWeight: 700,
      letterSpacing: '-0.04em',
    },
    h4: {
      fontWeight: 700,
      letterSpacing: '-0.03em',
    },
    h5: {
      fontWeight: 700,
    },
    button: {
      fontWeight: 700,
      textTransform: 'none',
    },
  },
})
