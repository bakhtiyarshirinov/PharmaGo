import Link from 'next/link'
import { Button, Card, CardContent, CardHeader, CardTitle, PageHeader, StatusBadge } from '@pharmago/ui'

export default function LandingPage() {
  return (
    <div className="space-y-8">
      <PageHeader
        eyebrow="Consumer pharmacy marketplace"
        title="Search medicines, compare nearby pharmacies, reserve in minutes."
        description="PharmaGo consumer app is built for a tight discovery flow: query, compare, reserve, track."
        actions={
          <>
            <Button asChild variant="secondary">
              <Link href="/medicines">Explore medicines</Link>
            </Button>
            <Button asChild variant="outline">
              <Link href="/pharmacies">Browse pharmacies</Link>
            </Button>
          </>
        }
      />

      <div className="grid gap-6 md:grid-cols-3">
        <Card>
          <CardHeader>
            <CardTitle>Discovery-first search</CardTitle>
          </CardHeader>
          <CardContent className="space-y-3 text-sm text-slate-600">
            <p>Strong medicine and pharmacy discovery surfaces with suggestions, cards and contextual availability.</p>
            <StatusBadge tone="info">MVP core</StatusBadge>
          </CardContent>
        </Card>
        <Card>
          <CardHeader>
            <CardTitle>Reservation tracking</CardTitle>
          </CardHeader>
          <CardContent className="space-y-3 text-sm text-slate-600">
            <p>From medicine detail to reservation timeline without dropping the user into a backoffice-looking flow.</p>
            <StatusBadge tone="success">Critical path</StatusBadge>
          </CardContent>
        </Card>
        <Card>
          <CardHeader>
            <CardTitle>Notifications and trust</CardTitle>
          </CardHeader>
          <CardContent className="space-y-3 text-sm text-slate-600">
            <p>Unread summary, inbox and reservation state visibility are built into the product, not bolted on.</p>
            <StatusBadge tone="warning">Post-login</StatusBadge>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}

