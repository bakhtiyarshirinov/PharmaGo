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
  status?: number
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

    if (!accessToken) {
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
        queryClient.invalidateQueries({ queryKey: ['notifications'] }),
      ])

    const invalidateNotifications = () =>
      queryClient.invalidateQueries({ queryKey: ['notifications'] })

    connection.on('reservation.status.changed', async (payload: ReservationRealtimePayload) => {
      await invalidateReservations()
      toast.info(`Reservation ${payload.reservationNumber} updated`)
    })

    connection.on('notification.received', async (payload: NotificationRealtimePayload) => {
      await invalidateNotifications()
      toast.info(payload.title || 'New notification', {
        description: payload.message,
      })
    })

    void connection.start().catch(() => {
      toast.error('Unable to connect realtime updates.')
    })

    return () => {
      connection.off('reservation.status.changed')
      connection.off('notification.received')
      void connection.stop()
    }
  }, [queryClient, session?.accessToken])

  return null
}
