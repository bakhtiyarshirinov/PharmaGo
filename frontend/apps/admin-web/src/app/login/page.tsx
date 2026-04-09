import { PageHeader, StatusBadge } from '@pharmago/ui'
import { AdminLoginForm } from '../../modules/auth/components/AdminLoginForm'

export default function LoginPage() {
  return (
    <div className="mx-auto flex min-h-screen max-w-6xl flex-col justify-center gap-10 px-6 py-16">
      <div className="grid gap-10 lg:grid-cols-[1.05fr,0.95fr] lg:items-center">
        <div className="space-y-6">
          <StatusBadge tone="info">PharmaGo Admin Control</StatusBadge>
          <PageHeader
            eyebrow="Панель модератора"
            title="Управление аптечной сетью, пользователями и операционным контуром."
            description="Admin-портал собирает глобальную картину платформы: сеть, сотрудники, активные резервы и качество данных по всем аптекам."
          />
        </div>

        <AdminLoginForm />
      </div>
    </div>
  )
}
