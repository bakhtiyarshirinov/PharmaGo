import { EmptyState, PageHeader } from '@pharmago/ui'

export default function NotificationsPage() {
  return (
    <div className="space-y-8">
      <PageHeader
        eyebrow="Ops inbox"
        title="Notification inbox for shift-critical events."
        description="Unread alerts, reservation state changes and stock pressure should surface here with fast actions."
      />
      <EmptyState title="Notifications scaffold" description="Connect notifications history, unread count and preferences here." />
    </div>
  )
}

