import Link from 'next/link'
import { Button, Card, CardContent, CardHeader, CardTitle, PageHeader, StatusBadge } from '@pharmago/ui'

export default function LandingPage() {
  return (
    <div className="space-y-8">
      <section className="consumer-glass overflow-hidden rounded-[2.25rem] px-6 py-8 md:px-10 md:py-12">
        <div className="grid gap-8 lg:grid-cols-[1.1fr,0.9fr] lg:items-end">
          <PageHeader
            eyebrow="Consumer pharmacy marketplace"
            title="Search medicines, compare nearby pharmacies, reserve with confidence."
            description="PharmaGo keeps the consumer journey fast and reassuring: discover, compare, reserve, then follow every status change without leaving the product context."
            actions={
              <>
                <Button asChild className="rounded-full" variant="secondary">
                  <Link href="/medicines">Explore medicines</Link>
                </Button>
                <Button asChild className="rounded-full" variant="outline">
                  <Link href="/pharmacies">Browse pharmacies</Link>
                </Button>
              </>
            }
          />

          <div className="grid gap-4 sm:grid-cols-2">
            <div className="rounded-[2rem] bg-slate-950 px-5 py-6 text-white shadow-2xl shadow-slate-950/15">
              <p className="text-xs uppercase tracking-[0.28em] text-white/60">Fast path</p>
              <p className="consumer-display mt-3 text-3xl font-semibold">2-minute reserve flow</p>
              <p className="mt-3 text-sm text-white/72">Find stock, choose a branch and open a reservation without dropping into a backoffice-style checkout.</p>
            </div>
            <div className="rounded-[2rem] bg-gradient-to-br from-emerald-500/10 via-white to-orange-500/10 px-5 py-6">
              <p className="text-xs uppercase tracking-[0.28em] text-slate-500">Trust layer</p>
              <p className="consumer-display mt-3 text-3xl font-semibold text-slate-950">Realtime status visibility</p>
              <p className="mt-3 text-sm text-slate-600">Confirmations, pickup readiness, expiry pressure and completion all stay visible in one narrative flow.</p>
            </div>
          </div>
        </div>
      </section>

      <div className="grid gap-6 md:grid-cols-3">
        <Card className="consumer-glass border-0 rounded-[2rem]">
          <CardHeader className="space-y-3">
            <StatusBadge tone="info">MVP core</StatusBadge>
            <CardTitle className="consumer-display text-2xl">Discovery-first search</CardTitle>
          </CardHeader>
          <CardContent className="space-y-3 text-sm leading-6 text-slate-600">
            <p>Strong medicine and pharmacy discovery surfaces with suggestions, pricing context and visible reservation posture before the user commits.</p>
          </CardContent>
        </Card>
        <Card className="consumer-glass border-0 rounded-[2rem]">
          <CardHeader className="space-y-3">
            <StatusBadge tone="success">Critical path</StatusBadge>
            <CardTitle className="consumer-display text-2xl">Reservation tracking</CardTitle>
          </CardHeader>
          <CardContent className="space-y-3 text-sm leading-6 text-slate-600">
            <p>The reservation timeline feels product-native and calm instead of forcing the user to interpret operational language.</p>
          </CardContent>
        </Card>
        <Card className="consumer-glass border-0 rounded-[2rem]">
          <CardHeader className="space-y-3">
            <StatusBadge tone="warning">Post-login</StatusBadge>
            <CardTitle className="consumer-display text-2xl">Notifications and trust</CardTitle>
          </CardHeader>
          <CardContent className="space-y-3 text-sm leading-6 text-slate-600">
            <p>Unread summary, inbox and reservation lifecycle visibility stay connected so the app feels dependable, not transactional and forgetful.</p>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
