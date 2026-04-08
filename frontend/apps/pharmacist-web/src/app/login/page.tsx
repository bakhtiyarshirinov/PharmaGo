import Link from 'next/link'
import { Button, Card, CardContent, CardHeader, CardTitle, PageHeader } from '@pharmago/ui'

export default function LoginPage() {
  return (
    <div className="mx-auto max-w-5xl px-6 py-16">
      <PageHeader
        eyebrow="Pharmacist operations"
        title="Pharmacist app login"
        description="Use the shared BFF auth route handlers, but only pharmacist role should be allowed into this portal."
      />
      <Card className="mt-8 max-w-md bg-slate-900 text-white">
        <CardHeader>
          <CardTitle>Portal access</CardTitle>
        </CardHeader>
        <CardContent>
          <Button asChild variant="secondary">
            <Link href="/api/auth/session">Connect session</Link>
          </Button>
        </CardContent>
      </Card>
    </div>
  )
}

