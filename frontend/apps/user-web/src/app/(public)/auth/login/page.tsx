import { PageHeader } from '@pharmago/ui'
import { LoginForm } from '../../../../modules/auth/components/LoginForm'

export default function LoginPage() {
  return (
    <div className="space-y-8">
      <PageHeader
        eyebrow="Authentication"
        title="Sign in to continue your reservation flow."
        description="Login is handled through the frontend BFF. Session state is refreshed through server auth routes, so the app restores your reservation flow without exposing cookie state to the browser."
      />
      <LoginForm />
    </div>
  )
}
