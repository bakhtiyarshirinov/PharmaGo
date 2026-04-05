import { AppProviders } from './app/AppProviders'
import { AppRouter } from './app/router'

function App() {
  return (
    <AppProviders>
      <AppRouter />
    </AppProviders>
  )
}

export default App
