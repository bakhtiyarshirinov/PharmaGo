import { EmptyState, PageHeader } from '@pharmago/ui'

export default function UsersPage() {
  return (
    <div className="space-y-8">
      <PageHeader eyebrow="Users" title="Users management" description="Paginated users table, detail drawer and role management should live here." />
      <EmptyState title="Users table scaffold" description="Wire users list, role change and restore flows here." />
    </div>
  )
}

