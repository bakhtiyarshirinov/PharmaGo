'use client'

import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { mapBackendRole, useAuthStore } from '@pharmago/auth/client'
import type { AuthSession } from '@pharmago/types'
import { Button, Card, CardContent, CardHeader, CardTitle, EmptyState, Input, PageHeader, StatusBadge } from '@pharmago/ui'
import { browserApi } from '../../lib/api'
import { formatDateTime } from '../../lib/format'
import { queryKeys } from '../../lib/query-keys'

interface AuditLogsScreenProps {
  initialSession?: AuthSession | null
}

export function AuditLogsScreen({ initialSession = null }: AuditLogsScreenProps) {
  const liveSession = useAuthStore((state) => state.session)
  const session = liveSession ?? initialSession
  const role = session?.user.role ? mapBackendRole(session.user.role) : 'guest'
  const [entityName, setEntityName] = useState('')
  const [action, setAction] = useState('')
  const [pharmacyId, setPharmacyId] = useState('')

  const auditLogs = useQuery({
    queryKey: queryKeys.audit.list({ pharmacyId, entityName, action }),
    queryFn: () =>
      browserApi.admin.auditLogs({
        pharmacyId: pharmacyId.trim() || undefined,
        entityName: entityName.trim() || undefined,
        action: action.trim() || undefined,
      }),
  })

  if (role !== 'admin') {
    return (
      <EmptyState
        title="Нет доступа к audit logs"
        description="Этот экран доступен только модератору платформы."
      />
    )
  }

  return (
    <div className="space-y-8">
      <PageHeader
        eyebrow="Audit"
        title="Последние операционные события и изменения данных."
        description="Read-only лента по ключевым действиям платформы: резервы, аптеки, пользователи и служебные события."
        actions={<StatusBadge tone="info">{auditLogs.data?.length ?? 0} событий</StatusBadge>}
      />

      <Card className="admin-glass border-white/60 bg-white/95">
        <CardHeader className="flex flex-col gap-4">
          <div className="space-y-1">
            <CardTitle>Фильтры аудита</CardTitle>
            <p className="text-sm text-slate-500">Фильтрация по сущности, action-name и аптеке.</p>
          </div>
          <div className="grid gap-3 lg:grid-cols-3">
            <Input value={entityName} onChange={(event) => setEntityName(event.target.value)} placeholder="Entity name, например Reservation" />
            <Input value={action} onChange={(event) => setAction(event.target.value)} placeholder="Action, например reservation.created" />
            <Input value={pharmacyId} onChange={(event) => setPharmacyId(event.target.value)} placeholder="PharmacyId (необязательно)" />
          </div>
        </CardHeader>
        <CardContent>
          {auditLogs.isLoading ? (
            <div className="space-y-3">
              {Array.from({ length: 6 }).map((_, index) => (
                <div key={index} className="h-24 animate-pulse rounded-[1.75rem] bg-slate-100" />
              ))}
            </div>
          ) : auditLogs.isError ? (
            <div className="rounded-[1.75rem] border border-red-200 bg-red-50 p-5 text-sm text-red-700">
              Не удалось загрузить audit logs.
            </div>
          ) : auditLogs.data?.length ? (
            <div className="space-y-3">
              {auditLogs.data.map((item) => (
                <div
                  key={item.id}
                  className="rounded-[1.75rem] border border-white/70 bg-white/85 p-4 shadow-[0_18px_50px_rgba(15,23,42,0.06)]"
                >
                  <div className="flex flex-col gap-3 lg:flex-row lg:items-start lg:justify-between">
                    <div className="space-y-2">
                      <div className="flex flex-wrap gap-2">
                        <StatusBadge tone="info">{item.entityName}</StatusBadge>
                        <StatusBadge tone="neutral">{item.action}</StatusBadge>
                        {item.pharmacyName ? <StatusBadge tone="success">{item.pharmacyName}</StatusBadge> : null}
                      </div>
                      <div>
                        <p className="font-medium text-slate-950">{item.description}</p>
                        <p className="mt-1 text-sm text-slate-500">
                          {item.userFullName || 'System'} · {formatDateTime(item.createdAtUtc)}
                        </p>
                      </div>
                    </div>
                    {item.entityId ? (
                      <div className="rounded-full border border-slate-200 px-3 py-1 text-xs text-slate-500">
                        {item.entityId}
                      </div>
                    ) : null}
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <EmptyState title="Событий не найдено" description="Уточни фильтры или подожди появления новых действий в системе." />
          )}
        </CardContent>
      </Card>
    </div>
  )
}
