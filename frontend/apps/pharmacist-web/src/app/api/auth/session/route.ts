import { sessionHandler } from '@pharmago/auth/server'

export async function GET() {
  return sessionHandler({ portal: 'pharmacist' })
}
