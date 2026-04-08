import { EmptyState, PageHeader } from '@pharmago/ui'

export default function LoginPage() {
  return (
    <div className="mx-auto max-w-5xl px-6 py-16">
      <PageHeader
        eyebrow="Admin control center"
        title="Admin portal login"
        description="Only moderator/admin role should ever enter this surface."
      />
      <EmptyState title="Admin login scaffold" description="Reuse the shared BFF auth flow and reject non-admin roles." />
    </div>
  )
}

