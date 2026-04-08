import { EmptyState, PageHeader } from '@pharmago/ui'

export default function AuditLogsPage() {
  return (
    <div className="space-y-8">
      <PageHeader eyebrow="Audit" title="Audit log review" description="Audit log UX should be table-first with filters and structured metadata inspectors." />
      <EmptyState title="Audit log scaffold" description="Connect audit logs pagination, filters and detail panels here." />
    </div>
  )
}

