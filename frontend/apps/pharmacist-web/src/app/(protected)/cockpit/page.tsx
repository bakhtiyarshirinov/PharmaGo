import { Card, CardContent, CardHeader, CardTitle, PageHeader } from '@pharmago/ui'

export default function CockpitPage() {
  return (
    <div className="space-y-8">
      <PageHeader
        eyebrow="Shift dashboard"
        title="Pharmacist cockpit"
        description="This page should aggregate active reservations, unread ops notifications and stock pressure."
      />
      <div className="grid gap-6 md:grid-cols-3">
        {['Active reservations', 'Unread notifications', 'Stock pressure'].map((title) => (
          <Card key={title} className="bg-slate-900 text-white">
            <CardHeader><CardTitle>{title}</CardTitle></CardHeader>
            <CardContent className="text-sm text-slate-300">Hook this card to the corresponding API module.</CardContent>
          </Card>
        ))}
      </div>
    </div>
  )
}

