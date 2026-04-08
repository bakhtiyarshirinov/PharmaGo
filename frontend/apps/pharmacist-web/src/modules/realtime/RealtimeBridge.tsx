'use client'

import { useEffect } from 'react'
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { useAuthStore } from '@pharmago/auth/client'
import { backendUrl } from '@pharmago/config'

type ReservationRealtimePayload = {
  reservationId: string
  reservationNumber: string
  pharmacyId: string
  status: number
}

type StockRealtimePayload = {
  stockItemId?: string
  pharmacyId: string
  medicineName?: string
}

type NotificationRealtimePayload = {
  reservationId?: string | null
  title: string
  message: string
}

function joinUrl(base: string, path: string) {
  return `${base.replace(/\/$/, '')}${path}`
}

export function RealtimeBridge() {
  const queryClient = useQueryClient()
  const session = useAuthStore((state) => state.session)

  useEffect(() => {
    const accessToken = session?.accessToken
    const pharmacyId = session?.user.pharmacyId

    if (!accessToken || !pharmacyId) {
      return
    }

    const connection = new HubConnectionBuilder()
      .withUrl(joinUrl(backendUrl, '/hubs/notifications'), {
        accessTokenFactory: () => accessToken,
        withCredentials: false,
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000])
      .configureLogging(LogLevel.Warning)
      .build()

    const invalidateReservations = () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: ['reservations'] }),
        queryClient.invalidateQueries({ queryKey: ['dashboard'] }),
        queryClient.invalidateQueries({ queryKey: ['inventory'] }),
      ])

    const invalidateInventory = () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: ['inventory'] }),
        queryClient.invalidateQueries({ queryKey: ['dashboard'] }),
      ])

    const invalidateNotifications = () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: ['notifications'] }),
        queryClient.invalidateQueries({ queryKey: ['dashboard'] }),
      ])

    connection.on('reservation.created', async (payload: ReservationRealtimePayload) => {
      await invalidateReservations()
      toast.info(`Новый резерв ${payload.reservationNumber}`)
    })

    connection.on('reservation.status.changed', async (_payload: ReservationRealtimePayload) => {
      await invalidateReservations()
      await invalidateNotifications()
    })

    connection.on('stock.low', async (payload: StockRealtimePayload) => {
      await invalidateInventory()
      toast.warning(
        payload.medicineName
          ? `Низкий остаток: ${payload.medicineName}`
          : 'Появился low-stock алерт',
      )
    })

    connection.on('stock.restored', async (_payload: StockRealtimePayload) => {
      await invalidateInventory()
    })

    connection.on('notification.received', async (payload: NotificationRealtimePayload) => {
      await invalidateNotifications()
      toast.info(payload.title || 'Новое уведомление', {
        description: payload.message,
      })
    })

    void connection.start().catch(() => {
      toast.error('Не удалось подключить realtime-канал аптеки.')
    })

    return () => {
      connection.off('reservation.created')
      connection.off('reservation.status.changed')
      connection.off('stock.low')
      connection.off('stock.restored')
      connection.off('notification.received')
      void connection.stop()
    }
  }, [queryClient, session?.accessToken, session?.user.pharmacyId])

  return null
}
