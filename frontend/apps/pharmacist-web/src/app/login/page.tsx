import { PageHeader, StatusBadge } from '@pharmago/ui'
import { PharmacistLoginForm } from '../../modules/auth/components/PharmacistLoginForm'

export default function LoginPage() {
  return (
    <div className="mx-auto flex min-h-screen max-w-6xl flex-col justify-center gap-10 px-6 py-16">
      <div className="grid gap-10 lg:grid-cols-[1.05fr,0.95fr] lg:items-center">
        <div className="space-y-6">
          <StatusBadge tone="warning">PharmaGo Pharmacist MVP</StatusBadge>
          <PageHeader
            eyebrow="Смена и операции"
            title="Очередь резервов, остатки и уведомления в одном рабочем окне."
            description="Портал оптимизирован под темп аптеки: быстрые подтверждения, выдача, контроль просадки склада и реакция на сервисные уведомления без переключения между системами."
          />
        </div>

        <PharmacistLoginForm />
      </div>
    </div>
  )
}
