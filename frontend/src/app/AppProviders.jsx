import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { CssBaseline, GlobalStyles, ThemeProvider } from '@mui/material'
import { useState } from 'react'
import { BrowserRouter } from 'react-router-dom'
import { appTheme } from './theme'

export function AppProviders({ children }) {
  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: {
            refetchOnWindowFocus: false,
            retry: 1,
            staleTime: 30_000,
          },
        },
      }),
  )

  return (
    <ThemeProvider theme={appTheme}>
      <CssBaseline />
      <GlobalStyles
        styles={{
          body: {
            background:
              'radial-gradient(circle at top left, rgba(239,108,63,0.16), transparent 24%), radial-gradient(circle at top right, rgba(31,122,101,0.18), transparent 26%), linear-gradient(180deg, #f5f1e8 0%, #f7f4ee 100%)',
            color: '#16211d',
          },
          a: {
            color: 'inherit',
            textDecoration: 'none',
          },
        }}
      />
      <QueryClientProvider client={queryClient}>
        <BrowserRouter>{children}</BrowserRouter>
      </QueryClientProvider>
    </ThemeProvider>
  )
}
