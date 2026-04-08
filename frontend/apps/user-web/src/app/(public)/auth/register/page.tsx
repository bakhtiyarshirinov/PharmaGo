import { EmptyState, PageHeader } from '@pharmago/ui'

export default function RegisterPage() {
  return (
    <div className="space-y-8">
      <PageHeader
        eyebrow="Registration"
        title="Registration form comes next."
        description="The shared auth layer already supports register in the API client. The product team should define the final onboarding form next."
      />
      <EmptyState
        title="Register screen placeholder"
        description="For the MVP starter, login is implemented first. Register should reuse the same BFF auth flow and validation stack."
      />
    </div>
  )
}

