import { PageHeader } from '@pharmago/ui'
import { LoginForm } from '../../../../modules/auth/components/LoginForm'

export default function LoginPage() {
  return (
    <div className="space-y-8">
      <PageHeader
        eyebrow="Authentication"
        title="Sign in to continue your reservation flow."
        description="Login is handled through the frontend BFF. Refresh token stays in an httpOnly cookie; access token stays in memory."
      />
      <LoginForm />
    </div>
  )
}

