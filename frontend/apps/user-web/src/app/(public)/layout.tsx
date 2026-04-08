import { AppShell } from '../../components/AppShell'

export default function PublicLayout({ children }: { children: React.ReactNode }) {
  return <AppShell>{children}</AppShell>
}

