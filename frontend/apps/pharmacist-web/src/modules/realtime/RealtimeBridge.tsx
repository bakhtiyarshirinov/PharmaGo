'use client'

import { useEffect } from 'react'
import { HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr'
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
  const setSession = useAuthStore((state) => state.setSession)

  useEffect(() => {
    const accessToken = session?.accessToken
    const pharmacyId = session?.user.pharmacyId
    const expiresAtUtc = session?.expiresAtUtc

    if (!accessToken || !pharmacyId) {
      return
    }

    if (isExpired(expiresAtUtc)) {
      setSession(null)
      return
    }

    let disposed = false
    let failureCount = 0

    const connection = new HubConnectionBuilder()
      .withUrl(joinUrl(backendUrl, '/hubs/notifications'), {
        accessTokenFactory: () => useAuthStore.getState().session?.accessToken ?? '',
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

    connection.onreconnected(() => {
      failureCount = 0
      void Promise.all([
        invalidateReservations(),
        invalidateInventory(),
        invalidateNotifications(),
      ])
    })

    const startConnection = async () => {
      if (disposed || connection.state !== HubConnectionState.Disconnected) {
        return
      }

      if (isSessionExpired()) {
        setSession(null)
        return
      }

      try {
        await connection.start()
        failureCount = 0
      } catch (error) {
        failureCount += 1

        if (disposed) {
          return
        }

        if (shouldInvalidateSession(error) || isSessionExpired()) {
          setSession(null)
          void connection.stop()
          return
        }

        if (failureCount >= 3) {
          toast.error('Не удалось подключить realtime-канал аптеки.')
        }

        window.setTimeout(() => {
          void startConnection()
        }, Math.min(10_000, failureCount * 2_000))
      }
    }

    void startConnection()

    return () => {
      disposed = true
      connection.off('reservation.created')
      connection.off('reservation.status.changed')
      connection.off('stock.low')
      connection.off('stock.restored')
      connection.off('notification.received')
      void connection.stop()
    }
    function isSessionExpired() {
      return isExpired(useAuthStore.getState().session?.expiresAtUtc)
    }
  }, [queryClient, session?.accessToken, session?.expiresAtUtc, session?.user.pharmacyId, setSession])

  return null
}

function isExpired(expiresAtUtc?: string | null) {
  if (!expiresAtUtc) {
    return false
  }

  const expiresAtMs = new Date(expiresAtUtc).getTime()
  return Number.isFinite(expiresAtMs) && expiresAtMs <= Date.now()
}

function shouldInvalidateSession(error: unknown) {
  if (!(error instanceof Error)) {
    return false
  }

  const message = error.message.toLowerCase()
  return message.includes('401') || message.includes('unauthorized')
}
