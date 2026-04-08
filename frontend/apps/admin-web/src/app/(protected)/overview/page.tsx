import { Card, CardContent, CardHeader, CardTitle, PageHeader } from '@pharmago/ui'

export default function AdminOverviewPage() {
  return (
    <div className="space-y-8">
      <PageHeader
        eyebrow="Admin overview"
        title="Control center for users, pharmacies and master data."
        description="Admin UI is desktop-first and table-heavy. This starter page defines the shell and information hierarchy."
      />
      <div className="grid gap-6 md:grid-cols-3">
        {['Users', 'Pharmacies', 'Master data'].map((title) => (
          <Card key={title}>
            <CardHeader><CardTitle>{title}</CardTitle></CardHeader>
            <CardContent className="text-sm text-slate-600">Attach paginated admin summaries here.</CardContent>
          </Card>
        ))}
      </div>
    </div>
  )
}

